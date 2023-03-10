using TMPro;
using UnityEngine;

namespace BattleScripts
{
    /// <summary>
    /// This Script is used inside of Battle Script, this is used to connect all of the UI elements, mostly working
    /// through the arrws, vicotry tab
    /// </summary>
    public class UIComponent : MonoBehaviour
    {
        //========= Menu Components 
        //Base Arrows 
        [HideInInspector] public GameObject[] baseArrows = new GameObject[2];
        [HideInInspector] public TextMeshProUGUI[] baseText = new TextMeshProUGUI[2];
        //Tab Arrows 
        [HideInInspector] public GameObject attackTab;
        [HideInInspector] public GameObject[] attackArrows = new GameObject[4];
        //Item Arrows 
        [HideInInspector] public GameObject itemTab;
        public GameObject[] itemArrows = new GameObject[4];
        //Items Count 
        [HideInInspector] public TextMeshProUGUI[] itemText = new TextMeshProUGUI[3];
        
        [HideInInspector] public GameObject canvas;
        //Victory Pop Up 
        [HideInInspector] public GameObject victoryTab;
        [HideInInspector] public TextMeshProUGUI defeated;
        [HideInInspector] public TextMeshProUGUI gold;
    
    
        //===== Sets Up UI =================================================================================================

        public void SetUpUI()
        {
            canvas = GameObject.Find("Canvas");
            victoryTab = GameObject.Find("Canvas").transform.Find("Victory_Pop_Up").gameObject;
            defeated = GameObject.Find("Canvas").transform.Find("Victory_Pop_Up").transform.Find("Defeated")
                .GetComponent<TextMeshProUGUI>();
            gold = GameObject.Find("Canvas").transform.Find("Victory_Pop_Up").transform.Find("Gold")
                .GetComponent<TextMeshProUGUI>();
        
            //Connects the Base Point Arrow
            for (var i = 0; i < baseArrows.Length; i++)
            {
                var path = "Point_" + i;
                var arrow = GameObject.Find("Canvas").transform.Find("Base_Player_Actions").Find(path).gameObject;
                baseArrows[i] = arrow;
            }

            for (var i = 0; i < baseText.Length; i++)
            {
                var path = "Text_" + i;
                var text = GameObject.Find("Canvas").transform.Find("Base_Player_Actions").Find(path).GetComponent<TextMeshProUGUI>();
                baseText[i] = text;
            }
        
            itemTab = GameObject.Find("Canvas").transform.Find("Item_Player_Actions").gameObject;
            itemTab.SetActive(false);
        
            //Connects the Item Point Arrows 
            for (var i = 0; i < itemArrows.Length; i++)
            {
                var path = "Point_" + i;
                var arrow = GameObject.Find("Canvas").transform.Find("Item_Player_Actions").Find(path).gameObject;
                itemArrows[i] = arrow;
            }
            for (var i = 0; i < itemText.Length; i++)
            {
                var path = "Item_" + i;
                var text = GameObject.Find("Canvas").transform.Find("Item_Player_Actions").Find(path).Find("Amount_Left").GetComponent<TextMeshProUGUI>();
                itemText[i] = text;
            }

            attackTab = GameObject.Find("Canvas").transform.Find("Attack_Player_Actions").gameObject;
            attackTab.SetActive(false);
        
            //Connects the Attack Point Arrows 
            for (var i = 0; i < attackArrows.Length; i++)
            {
                var path = "Point_" + i;
                var arrow = GameObject.Find("Canvas").transform.Find("Attack_Player_Actions").Find(path).gameObject;
                attackArrows[i] = arrow;
            }
        }
    
    }
}
