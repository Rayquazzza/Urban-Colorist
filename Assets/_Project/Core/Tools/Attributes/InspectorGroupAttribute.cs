using System;
using UnityEngine;


    /// <summary>
    /// An attribute used to group inspector fields under common dropdowns
    /// Implementation inspired by Rodrigo Prinheiro's work, available at https://github.com/RodrigoPrinheiro/unityFoldoutAttribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
    public class InspectorGroupAttribute : PropertyAttribute
    {
        public string GroupName;
        public bool GroupAllFieldsUntilNextGroupAttribute;
        public int GroupColorIndex;
        public bool ClosedByDefault;

        public InspectorGroupAttribute(string groupName, bool groupAllFieldsUntilNextGroupAttribute = false, int groupColorIndex = 24, bool closedByDefault = false)
        {
            if (groupColorIndex > 139) { groupColorIndex = 139; }

            this.GroupName = groupName;
            this.GroupAllFieldsUntilNextGroupAttribute = groupAllFieldsUntilNextGroupAttribute;
            this.GroupColorIndex = groupColorIndex;
            this.ClosedByDefault = closedByDefault;
        }
    }
