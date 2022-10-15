using System;

#if ODIN_INSPECTOR
#else
namespace Sirenix.OdinInspector
{
    public class LabelText : Attribute
    {
        public LabelText(string s)
        {

        }
    }

    public class InfoBox : Attribute
    {
        public InfoBox(string s)
        {

        }
    }

    public class ListDrawerSettings : Attribute
    {
        public bool HideAddButton;
        public bool HideRemoveButton;
        public string ListElementLabelName;
        public string CustomAddFunction;
        public string CustomRemoveIndexFunction;
        public string CustomRemoveElementFunction;
        public string OnBeginListElementGUI;
        public string OnEndListElementGUI;
        public bool AlwaysAddDefaultValue;
        public bool AddCopiesLastElement;
        public string ElementColor;
        public bool ShowIndexLabels; 
    }
}
#endif