using System;
using System.Collections.Generic;
using System.Reflection;
using StudioElevenLib.Collections;
using StudioElevenLib.Level5.Binary.Logic;
using StudioElevenLib.Level5.Binary.Mapper;

namespace StudioElevenLib.Level5.Binary.Collections
{
    /// <summary>
    /// A specialized tree node class for Entry items with convenient access to CfgBinMapper methods.
    /// </summary>
    public class CfgTreeNode : TreeNode<Entry>
    {
        public CfgTreeNode(Entry item, int level = 0) : base(item, level)
        {

        }

        public CfgTreeNode FindByName(string name)
        {
            if (Item.Name == name) return this;

            foreach (var child in Children)
            {
                if (child is CfgTreeNode cfgChild)
                {
                    var result = cfgChild.FindByName(name);
                    if (result != null) return result;
                }
            }
            return null;
        }

        public void TraverseEntries(Action<Entry> action)
        {
            action(Item);
            foreach (var child in Children)
            {
                if (child is CfgTreeNode cfgChild)
                    cfgChild.TraverseEntries(action);
            }
        }

        #region Mapper Delegates

        /// <summary>
        /// Flattens this tree into a list of class instances using the mapper.
        /// </summary>
        public List<T> FlattenEntryToClassList<T>(string targetEntryName) where T : class, new()
        {
            return CfgBinMapper.FlattenEntryToClassList<T>(this, targetEntryName);
        }

        /// <summary>
        /// Rebuilds this tree from a list of class instances using the mapper.
        /// This replaces the content of the current node with the new structure.
        /// </summary>
        public void BuildEntryFromClassList<T>(List<T> instances, string parentEntryName, string childEntryName) where T : class
        {
            // Use the mapper to generate a new temporary CfgTreeNode
            var newTree = CfgBinMapper.BuildEntryFromClassList(instances, parentEntryName, childEntryName);

            // Replaces the data in this node with that from the generated tree
            Item.Name = newTree.Item.Name;
            Item.Variables.Clear();
            Item.Variables.AddRange(newTree.Item.Variables);

            Children.Clear();
            foreach (var child in newTree.Children)
            {
                AddChild(child);
            }
        }

        /// <summary>
        /// Adds a bounded entry (BEGIN ... END) to this node from a class list using the mapper.
        /// Creates a BEGIN entry with custom variables (or count variable by default) and an END entry automatically.
        /// </summary>
        public void AddBoundedEntryFromClassList<T>(
            List<T> instances,
            string parentEntryName,
            string childEntryName,
            List<Variable> variables = null
        ) where T : class
        {
            // Creates the BEGIN node from the mapper
            var beginNode = CfgBinMapper.BuildEntryFromClassList(instances, parentEntryName, childEntryName);

            // Use custom variables if provided, otherwise create a default count variable
            if (variables != null)
            {
                beginNode.Item.Variables = variables;
            }
            else
            {
                beginNode.Item.Variables = new List<Variable>()
                {
                    new Variable(CfgValueType.Int, instances.Count)
                };
            }

            // Adds BEGIN to this node
            AddChild(beginNode);

            // Adds the corresponding END
            AddChild(new CfgTreeNode(new Entry(parentEntryName.Replace("_BEGIN", "_END"))));
        }

        #endregion
    }
}