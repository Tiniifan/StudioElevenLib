using System;
using System.Collections.Generic;
using StudioElevenLib.Collections;
using StudioElevenLib.Level5.Binary.Logic;
using StudioElevenLib.Level5.Binary.Mapper;

namespace StudioElevenLib.Level5.Binary.Collections
{
    /// <summary>
    /// Specialized tree node class for PTREE structures.
    /// </summary>
    public class PtreeNode : TreeNode<Entry>
    {
        public PtreeNode(Entry item, int level = 0) : base(item, level)
        {
        }

        /// <summary>
        /// Gets the Header value (Variable[0].Value) safely.
        /// </summary>
        public string Header
        {
            get
            {
                if (Item.Variables != null && Item.Variables.Count > 0)
                    return Item.Variables[0]?.Value?.ToString();
                return null;
            }
        }

        /// <summary>
        /// Gets the Value (Variable[1].Value) safely.
        /// </summary>
        public string Value
        {
            get
            {
                if (Item.Variables != null && Item.Variables.Count > 1)
                    return Item.Variables[1]?.Value?.ToString();
                return null;
            }
        }

        /// <summary>
        /// Gets a variable value at the specified index and converts it to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to convert the value to</typeparam>
        /// <param name="index">The index of the variable to retrieve</param>
        /// <returns>The converted value, or default(T) if conversion fails or index is out of range</returns>
        public T GetValue<T>(int index)
        {
            try
            {
                if (Item.Variables != null && index >= 0 && index < Item.Variables.Count)
                {
                    var variable = Item.Variables[index];
                    if (variable?.Value != null)
                    {
                        return (T)Convert.ChangeType(variable.Value, typeof(T));
                    }
                }
            }
            catch (Exception)
            {
                // Conversion failed or other error
            }

            return default(T);
        }

        public T GetValueFromChild<T>(string value)
        {
            PtreeNode childNode = FindByValue(value);
            if (childNode == null)
                return default(T);

            object val = childNode.GetValue<object>(0);

            if (val == null)
                return default(T);

            return (T)Convert.ChangeType(val, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T));
        }

        public T GetValueFromChildByHeader<T>(string header, int valueIndex = 0)
        {
            PtreeNode baseNode = FindByHeader(header);
            if (baseNode == null || baseNode.Children.Count == 0)
                return default(T);

            PtreeNode child = baseNode.Children[0] as PtreeNode;
            if (child == null)
                return default(T);

            object val = child.GetValue<object>(valueIndex);

            if (val == null)
                return default(T);

            return (T)Convert.ChangeType(val, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T));
        }

        #region Find Methods

        public PtreeNode FindByHeader(string header)
        {
            if (Header == header) return this;

            foreach (var child in Children)
            {
                if (child is PtreeNode ptreeChild)
                {
                    var result = ptreeChild.FindByHeader(header);
                    if (result != null) return result;
                }
            }
            return null;
        }

        public PtreeNode FindByValue(string value)
        {
            if (Value == value) return this;

            foreach (var child in Children)
            {
                if (child is PtreeNode ptreeChild)
                {
                    var result = ptreeChild.FindByValue(value);
                    if (result != null) return result;
                }
            }
            return null;
        }

        public PtreeNode FindByHeaderAndValue(string header, string value)
        {
            if (Header == header && Value == value)
                return this;

            foreach (var child in Children)
            {
                if (child is PtreeNode ptreeChild)
                {
                    var result = ptreeChild.FindByHeaderAndValue(header, value);
                    if (result != null)
                        return result;
                }
            }
            return null;
        }

        #endregion

        #region Traversal

        public void TraverseEntries(Action<Entry> action)
        {
            action(Item);
            foreach (var child in Children)
            {
                if (child is PtreeNode ptreeChild)
                    ptreeChild.TraverseEntries(action);
            }
        }

        #endregion

        public new string PrintTree(int level = 0)
        {
            string ret = new string('\t', level) + string.Format("- {0} (Level: {1})\n", Header, Level);
            foreach (var child in Children)
            {
                ret += (child as PtreeNode).PrintTree(level + 1);
            }
            return ret;
        }
    }
}