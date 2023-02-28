using System;
using System.Collections;
using System.Collections.Generic;
using Base;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace BattleScripts
{
    public class BattleScript : MonoBehaviour
    {
        //======== Data
        private Data _data;
        private UnitsAndAttacks _unitsAndAttacksScript;

        //========= State Machine Components 
        private enum BattleStates
        {
            Start,
            PlayerTurn,
            EnemyTurn,
            End
        }
        private BattleStates _currentState = BattleStates.Start;
    
        //======== Set Up
        private EnemyComponents _eC; //Enemy Visual Components and Controls 
        private PlayerComponents _pC; //Player Visual Components and Controls 
        private UIComponent _uC; //UI Visual Components and Controls 

        //======== Controls
        private enum MenuState
        {
            Base,
            Attack,
            Item
        }
        private MenuState _currentMenu = MenuState.Base;
    
    
        private int _baseIndex;       //Which base option is the player looking at 
        private int _attackIndex;     //Which attack is the player looking at
        private int _itemIndex;
        private bool _enemyTurn;
        private Animator _battleAnimator;
        [SerializeField] private GameObject damageNumber;

        private int _barSlider = 0;

        private AudioSource _moveAudioSource;
        private AudioSource _selectAudioSource;
        private AudioSource _denyAudioSource;
        private AudioSource _attackAudioSource;

        //======== Consts 
        private const float TimeBarLowering = 0.035f;
        private const float TimeEnemyThinkTime = 2f;
        private const float TimeTillEnemyTurn = 1f;

        private const float TimePlayerHurt = 2f; 
        
        // Start is called before the first frame update
        private void Start()
        {
        
            SetUpBattle();
            _currentState = BattleStates.PlayerTurn;

            _moveAudioSource = GameObject.Find("SFX").transform.Find("MovingSFX").GetComponent<AudioSource>();
            _selectAudioSource = GameObject.Find("SFX").transform.Find("SelectSFX").GetComponent<AudioSource>();
            _denyAudioSource = GameObject.Find("SFX").transform.Find("DenySFX").GetComponent<AudioSource>();
            _attackAudioSource = GameObject.Find("SFX").transform.Find("AttackSFX").GetComponent<AudioSource>();
        }

        private void SetUpBattle()
        {
            _data = GameObject.Find("Data").GetComponent<Data>();
            _unitsAndAttacksScript = GameObject.Find("BattleScript").GetComponent<UnitsAndAttacks>();
        
            _pC = GameObject.Find("BattleScript").GetComponent<PlayerComponents>();
            _pC.SetUpPlayer(_data.GetUnit());
        
            _eC = GameObject.Find("BattleScript").GetComponent<EnemyComponents>();
            _eC.SetUpEnemy(_unitsAndAttacksScript, _data.GetId());

            _uC = GameObject.Find("BattleScript").GetComponent<UIComponent>();
            _uC.SetUpUI();
            _uC.itemText[0].text = _data.GetItem(0).ToString();
            _uC.itemText[1].text = _data.GetItem(1).ToString();
            _uC.itemText[2].text = _data.GetItem(2).ToString();

            _battleAnimator = GameObject.Find("Canvas").GetComponent<Animator>();
        }

        //==================================================================================================================
        // Game State Methods 
        //==================================================================================================================
    
        private void Update()
        {
            switch (_currentState)
            {
                case BattleStates.PlayerTurn:
                {
                    PlayerActions();
                    break;
                }
                case BattleStates.EnemyTurn:
                {
                    if (_enemyTurn)
                    {
                        _enemyTurn = false;
                        StartCoroutine(EnemyAction());
                    }
                    break;
                }
                case BattleStates.End:
                {
                    if (Input.GetKeyDown(KeyCode.Space))
                    {
                        SceneManager.LoadScene("StartScene");
                    }
                    break;
                }
                case BattleStates.Start:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    
    
        //==================================================================================================================
        //Player Actions 
        //==================================================================================================================

        private void PlayerActions()
        {
            switch (_currentMenu)
            {
                case MenuState.Base:
                {
                    _baseIndex = MenuScroll(_uC.baseArrows, _baseIndex);
                    PlayerBaseAction();
                    break;
                }
                case MenuState.Attack:
                {
                    _attackIndex = MenuScroll(_uC.attackArrows, _attackIndex);
                    PlayerAttackActions();
                    break;
                }
                case MenuState.Item:
                {
                    _itemIndex = MenuScroll(_uC.itemArrows, _itemIndex);
                    PlayerItemActions();
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        //==================================================================================================================
        //Menu Scroll Methods  
        //==================================================================================================================
        
        /// <summary>
        /// Used to Navigate the menus, moves the arrow left, right for base arrows and up, down for attack and items
        /// It takes in the arrays of arrow UI elements that need to be update and the current index and then
        /// returns that same index once it's been updated. 
        /// </summary>
        /// <param name="arrows"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private int MenuScroll(IReadOnlyList<GameObject> arrows, int index)
        {
            //If Player Goes Left or Down then the index decrements, if the player goes Right or Up the index decrements 
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                index = UpdateArrowIndex(false, arrows, index);
            }
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                index = UpdateArrowIndex(true, arrows, index);
            }
            return index;
        }

        /// <summary>
        /// Increases or decrement the value of the index, checks if it's still within the
        /// array bounds, play and SFX and updates the Arrow UI elements. 
        /// </summary>
        /// <param name="inc"></param>
        /// <param name="arrows"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private int UpdateArrowIndex(bool inc, IReadOnlyList<GameObject> arrows, int index)
        {
            //Updates the number based on if we're incrementing or not
            index = inc ? index - 1 : index + 1;
            //Checks to see if it's out of bounds, if it is fixes it
            if (index < 0) { index = arrows.Count - 1; }
            else if (index == arrows.Count) { index = 0; }
            //Plays SFX and Update the UI 
            _moveAudioSource.Play();
            UpdateArrows(arrows, index);
            //Returns the index 
            return index;
        }
        
        /// <summary>
        /// Turns off all of the UI elements except for the one that the index is on 
        /// </summary>
        /// <param name="arrows"></param>
        /// <param name="index"></param>
        private static void UpdateArrows(IReadOnlyList<GameObject> arrows, int index)
        {
            //Turns off all the Images 
            foreach (var arrow in arrows) { arrow.SetActive(false); }
            //Turns on the only one that should be on
            arrows[index].SetActive(true);
        }
        
        //==============================================================================================================
        //Player Action Methods Methods  
        //==============================================================================================================
        
        /// <summary>
        /// Based player action, allows the player to either turn on the attack or item tab 
        /// </summary>
        private void PlayerBaseAction()
        {
            if (!Input.GetKeyDown(KeyCode.Space)) return;
            switch (_baseIndex)
            {
                case 0:
                    MoveTabs(MenuState.Attack, _uC.attackTab, true);
                    break;
                case 1:
                    MoveTabs(MenuState.Item, _uC.itemTab, true);
                    break;
            }
        }

        /// <summary>
        /// Actions the player can take while looking at the attack tab 
        /// </summary>
        private void PlayerAttackActions()
        {
            if (!Input.GetKeyDown(KeyCode.Space)) return;
            
            //Move back to the base menu if on the last choice 
            if (_attackIndex == _uC.attackArrows.Length - 1) { MoveTabs(MenuState.Base, _uC.attackTab, false); }
            //Else choose an attack, depending on if there is an attack or if the player has enough 
            //mana execute the attack, otherwise play a fail SFX 
            else 
            {
                if (_attackIndex < _pC.maxPlayerAttackIndex &&
                    _pC.playerUnit.currentMana >= _pC.playerUnit.attacks[_attackIndex].manaCost)
                {
                    MoveTabs(MenuState.Base, _uC.attackTab, false);
                    _selectAudioSource.Play();
                    StartCoroutine(PlayerAttack());
                }
                else
                {
                    _denyAudioSource.Play();
                }
            }
        }

        /// <summary>
        /// Actions player can take while in the Item Menu 
        /// </summary>
        private void PlayerItemActions()
        {
            if (!Input.GetKeyDown(KeyCode.Space)) return;
            
            //If on the last index goes back to the base tab 
            if(_itemIndex ==  _uC.itemArrows.Length - 1) { MoveTabs(MenuState.Base, _uC.itemTab, false); }
            //Else checks if the player has at least one of the item, if so uses it else gets fail SFX 
            else 
            {
                if (_data.GetItem(_itemIndex) >= 1){
                    //Update the item count in Data 
                    _data.SubItem(_itemIndex);
                    //Update UI of the text 
                    _uC.itemText[_itemIndex].text = _data.GetItem(_itemIndex).ToString();
                    //Picks the effect based on which item was used 
                    switch (_itemIndex)
                    {
                        case 0:
                            IncreasePlayerHealth();
                            break;
                        case 1:
                            IncreasePlayerMagic();
                            break;
                        case 2:
                            StartCoroutine(SetUpEscapeScreen());
                            break;
                    }
                    _selectAudioSource.Play();
                    //Moves back to the Base Tab 
                    MoveTabs(MenuState.Base, _uC.itemTab, false);
                }
                else { _denyAudioSource.Play(); }   
            }
        }
        
        /// <summary>
        /// Update the state of the Tab State Machine, updates the visuals of the tab being on or of
        /// and rests the indexes to 0. 
        /// </summary>
        /// <param name="state"></param>
        /// <param name="tab"></param>
        /// <param name="turnOn"></param>
        private void MoveTabs(MenuState state, GameObject tab, bool turnOn)
        {
            _currentMenu = state;
            tab.SetActive(turnOn);
            _selectAudioSource.Play();
            ResetArrows();
        }

        /// <summary>
        /// Resets the indexes to 0 and update the UI elements to reflect that 
        /// </summary>
        private void ResetArrows()
        {
            _baseIndex = 0;
            UpdateArrows(_uC.baseArrows, _baseIndex);
            _attackIndex = 0;
            UpdateArrows(_uC.attackArrows, _attackIndex);
            _itemIndex = 0;
            UpdateArrows(_uC.itemArrows, _itemIndex);
        }
        
        //==============================================================================================================
        //Player/Enemy Attack Methods  
        //==============================================================================================================
        
        /// <summary>
        /// What Occurs when the Player attacks 
        /// </summary>
        /// <returns></returns>
        private IEnumerator PlayerAttack()
        {
            //Starts the Animation of Attacking Enemy 
            _battleAnimator.Play("EnemyHurt");
            
            //Rolls the damage and takes away player mana
            RollDamage(_pC.playerUnit.attacks, _attackIndex, _eC.enemyDamageNumberSpawnPoint, true);
            LowerPlayerMagic();
            
            //Takes away player controls over menu
            PlayerControls(BattleStates.EnemyTurn, false, Color.gray);

            //Gives a few second for the enemy to "think" 
            yield return new WaitForSeconds(TimeEnemyThinkTime);
            //If the enemy died start the win sequence 
            if (_eC.enemyCurrentHealth <= 0)
            {
                yield return new WaitForSeconds(TimeTillEnemyTurn / 2f);
                _selectAudioSource.Play();
                StartCoroutine(WinAction());
            }
            //Else give hte power to enemy 
            else { _enemyTurn = true; }
        }
        
        /// <summary>
        /// What occurs when an enemy attacks 
        /// </summary>
        /// <returns></returns>
        private IEnumerator EnemyAction()
        {
            //Picks a random index for enemy to attack from 
            var rollAttack = Random.Range(0, _eC.maxEnemyAttackIndex);
            //Performs the attack 
            RollDamage(_eC.enemyUnit.attacks, rollAttack, _pC.playerDamageNumberSpawnPoint, false);
            //Plays animation that shows player hurt 
            _battleAnimator.Play("Shake");
            //Waits for damage to be done 
            yield return new WaitForSeconds(TimePlayerHurt);
            //If player dies start the Lose Sequence 
            if (_pC.playerUnit.currentHealth <= 0) { StartCoroutine(LoseAction()); }
            //Else Let Player Attack again 
            else { PlayerControls(BattleStates.PlayerTurn, true, Color.black); }
        }

        //==================================================================================================================
        //Player/Enemy Attack Support Methods  
        //==================================================================================================================

        /// <summary>
        /// Rolls the damage and inflicts it on the opposite party 
        /// </summary>
        /// <param name="attacksList"></param>
        /// <param name="index"></param>
        /// <param name="point"></param>
        /// <param name="player"></param>
        private void RollDamage(IReadOnlyList<Attacks> attacksList, int index, GameObject point, bool player)
        {
            //Rolls the damage between 60% of the max value to max value 
            var damage = (int) Random.Range(attacksList[index].damage * 0.6f, attacksList[index].damage);
            
            //Spawns a floating number on the screen 
            SpawnDamage(point, "" + damage);
            
            //Plays Attack SFX
            _attackAudioSource.Play();
            
            //If Player is attacking lower enemy health, else vice versa 
            if (player) { LowerEnemyHealth(damage); }
            else { LowerPlayerHealth(damage); }
        }
        
        /// <summary>
        /// Instantiates a floating number near the enemy or player  
        /// </summary>
        /// <param name="point"></param>
        /// <param name="damage"></param>
        private void SpawnDamage(GameObject point, string damage)
        {
            var clone = Instantiate(damageNumber, point.transform.position, Quaternion.identity);
            clone.transform.SetParent(_uC.canvas.transform);
            clone.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = damage;
        }

        /// <summary>
        /// Updates the state the game is in and changes the color of text and arrows
        /// to indicate if the player is controlling the menus  
        /// </summary>
        /// <param name="battleState"></param>
        /// <param name="arrows"></param>
        /// <param name="color"></param>
        private void PlayerControls(BattleStates battleState, bool arrows, Color color)
        {
            _currentState = battleState;
            _uC.baseArrows[0].SetActive(arrows);
            foreach (var text in _uC.baseText) { text.color = color; }
        }

        
        //==================================================================================================================
        //UI Bar Update Methods 
        //==================================================================================================================
       
        /// <summary>
        /// Lowers the enemy's health and updates the UI element 
        /// </summary>
        /// <param name="damage"></param>
        private void LowerEnemyHealth(int damage)
        {
            //Get goal
            var goal = _eC.enemyCurrentHealth - damage;
            //Update the visual 
            StartCoroutine(UpdateBar(goal, _eC.enemyCurrentHealth, _eC.enemyCurrentHealth, _eC.enemyUnit.maxHealth, -1,
                _eC.enemyHealthImage));
            //Update the data 
            _eC.enemyCurrentHealth = goal;
        }
        
        /// <summary>
        /// Lowers the player's mana and updates the UI element 
        /// </summary>
        private void LowerPlayerMagic()
        {
            //Get the goal
            var goal = _pC.playerUnit.currentMana - _pC.playerUnit.attacks[_attackIndex].manaCost;
            //Update the Visuals 
            StartCoroutine(UpdateBar(_pC.playerUnit.currentMana, goal, _pC.playerUnit.currentMana,
                _pC.playerUnit.maxMana, -1, _pC.playerManaImage));
            _pC.playerManaText.text = goal + "/" + _pC.playerUnit.maxMana;
            //Update the Data 
            _pC.playerUnit.currentMana = goal;
        }

        /// <summary>
        /// Increases the player's mana and updates the UI element 
        /// </summary>
        private void IncreasePlayerMagic()
        {
            //Update the Visuals 
            StartCoroutine(UpdateBar(_pC.playerUnit.currentMana, _pC.playerUnit.maxMana, _pC.playerUnit.currentMana,
                _pC.playerUnit.maxMana, 1, _pC.playerManaImage));
            _pC.playerManaText.text = _pC.playerUnit.maxMana + "/" + _pC.playerUnit.maxMana;
            //Update the Data 
            _pC.playerUnit.currentMana = _pC.playerUnit.maxMana;

        }
    
        /// <summary>
        /// Increases the player's health and updates the UI element 
        /// </summary>
        private void IncreasePlayerHealth()
        {
            StartCoroutine(UpdateBar(_pC.playerUnit.currentHealth, _pC.playerUnit.maxHealth,
                _pC.playerUnit.currentHealth, _pC.playerUnit.maxHealth, 1, _pC.playerHealthImage));

            _pC.playerUnit.currentHealth = _pC.playerUnit.maxHealth;
            _pC.playerHealthText.text = _pC.playerUnit.currentHealth + "/" + _pC.playerUnit.maxHealth;
        }
        
        /// <summary>
        /// Lowers the player's health and updates the UI element 
        /// </summary>
        private void LowerPlayerHealth(int damage)
        {
            var goal = _pC.playerUnit.currentHealth - damage;

            StartCoroutine(UpdateBar(goal, _pC.playerUnit.currentHealth, _pC.playerUnit.currentHealth, _pC.playerUnit.maxHealth, -1,
                _pC.playerHealthImage));

            _pC.playerUnit.currentHealth = goal;
            _pC.playerHealthText.text = goal + "/" + _pC.playerUnit.maxHealth;
        }

        /// <summary>
        /// Runs the for loop that will lower the bar by +/- 1 increment every TimeBarLowering
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="current"></param>
        /// <param name="max"></param>
        /// <param name="inc"></param>
        /// <param name="image"></param>
        /// <returns></returns>
        private static IEnumerator UpdateBar(int start, int end, int current, int max, int inc, Image image)
        {
            for (var i = start; i < end; i++)
            {
                current += inc;
                image.fillAmount = (float) current / max;
                yield return new WaitForSeconds(TimeBarLowering);
            }
        }

        //==================================================================================================================
        //End Actions 
        //==================================================================================================================

        private IEnumerator WinAction()
        {
            _battleAnimator.Play("EnemyFade");
            if (_data.GetLifeGoal() == 1 || _data.GetLifeGoal() == 2)
            {
                _data.UpdateGoal(1);
            }
            yield return new WaitForSeconds(TimeTillEnemyTurn * 2);
            SetUpVictoryScreen(true);
            _data.SetUnit(_pC.playerUnit);
            yield return new WaitForSeconds(TimeTillEnemyTurn);
            _currentState = BattleStates.End;
        }

        private IEnumerator LoseAction()
        {
            _battleAnimator.Play("Swap");
            _data.ResetGoalState();
            yield return new WaitForSeconds(0.25f);
            SetUpVictoryScreen(false);
            _eC.enemyImage.sprite = _pC.playerUnit.sprite;
            _pC.playerIcon.sprite = _eC.enemyUnit.sprite;
            UpdatePlayerData();
            _eC.enemyUnit.currentHealth = _eC.enemyCurrentHealth;
            _data.SetUnit(_eC.enemyUnit);
            _data.ResetItems();
            yield return new WaitForSeconds(TimeTillEnemyTurn);
            _battleAnimator.Play("EnemyFade");
            yield return new WaitForSeconds(TimeTillEnemyTurn);
            _currentState = BattleStates.End;
        }
    
        private void UpdatePlayerData()
        {
            _pC.playerNameText.text = _eC.enemyUnit.unitName;
            _pC.playerHealthImage.fillAmount = (float) _eC.enemyCurrentHealth / _eC.enemyUnit.maxHealth;
            _eC.enemyHealthImage.fillAmount = (float) _pC.playerUnit.currentHealth / _pC.playerUnit.maxHealth;
            _pC.playerManaImage.fillAmount =  (float) _eC.enemyUnit.currentMana / _eC.enemyUnit.currentMana;
            _pC.playerHealthText.text = _eC.enemyCurrentHealth + "/" + _eC.enemyUnit.maxHealth;
            _pC.playerManaText.text = _eC.enemyUnit.currentMana+ "/" + _eC.enemyUnit.currentMana;

        }

        private void SetUpVictoryScreen(bool playerWon)
        {
            if (playerWon)
            {
                _uC.defeated.text = "You have defeated " + _eC.enemyUnit.unitName + " may they rest in peace.";
                _uC.gold.text = "You gather " +  _eC.enemyUnit.moneyDrop + " gold pieces.";
                if (_data.GetLifeGoal() == 3)
                {
                    _data.UpdateGoal(_eC.enemyUnit.moneyDrop);
                }
                _data.AddToStory(_pC.playerUnit.unitName + " have defeated " + _eC.enemyUnit.unitName + " may they rest in peace.");
                if (_data.GetIsGoalCompleted())
                {
                    _data.AddToStory(_pC.playerUnit.unitName + " completed their life goal of: " + _data.GetGoalText());
                    _data.SetPlayerDone(true);
                }
            }
            else
            {
                _uC.defeated.text = "You have defeated " + _pC.playerUnit.unitName + " may they rest in peace.";
                _uC.gold.text = "You gather " +  _data.GetMoney() + " gold pieces.";
                if (!_data.GetIsGoalCompleted())
                {
                    _data.AddToStory(_pC.playerUnit.unitName + " failed their life goal of: " + _data.GetGoalText());
                }
            }
            _uC.victoryTab.SetActive(true);
            if (playerWon) { _data.AddMoney(_eC.enemyUnit.moneyDrop); }
        }
    
        private IEnumerator SetUpEscapeScreen()
        {
            _uC.defeated.text = "You have escaped from " + _eC.enemyUnit.unitName + " in a cloud of smoke.";
            _data.AddToStory(_pC.playerUnit.unitName + " escaped from " + _eC.enemyUnit.unitName + ".");
            if (_data.GetLifeGoal() == 6)
            {
                _data.UpdateGoal(1);
            }
            if (_data.GetIsGoalCompleted())
            {
                _data.AddToStory(_pC.playerUnit.unitName + " completed their life goal of: " + _data.GetGoalText());
                _data.SetPlayerDone(true);
            }
            _uC.gold.text = "You get to live another day.";
            yield return new WaitForSeconds(TimeTillEnemyTurn);
            _currentState = BattleStates.End;
            _uC.victoryTab.SetActive(true);
        }
    }
}