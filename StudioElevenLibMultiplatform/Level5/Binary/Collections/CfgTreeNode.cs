using System;
using System.Collections;
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

        /// <summary>
        /// Checks if an entry with the specified name exists in this node or any of its descendants.
        /// </summary>
        /// <param name="name">The name of the entry to search for.</param>
        /// <returns>True if an entry with the specified name exists; otherwise, false.</returns>
        public bool Exists(string name)
        {
            if (Item.Name == name) return true;

            foreach (var child in Children)
            {
                if (child is CfgTreeNode cfgChild)
                {
                    if (cfgChild.Exists(name)) return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Deletes the first entry with the specified name from this node or any of its descendants.
        /// </summary>
        /// <param name="name">The name of the entry to delete.</param>
        /// <returns>True if an entry was found and deleted; otherwise, false.</returns>
        public bool Delete(string name)
        {
            for (int i = 0; i < Children.Count; i++)
            {
                if (Children[i] is CfgTreeNode cfgChild)
                {
                    if (cfgChild.Item.Name == name)
                    {
                        Children.RemoveAt(i);
                        return true;
                    }

                    if (cfgChild.Delete(name))
                    {
                        return true;
                    }
                }
            }
            return false;
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
        /// Dynamically flattens a configuration entry into a sequence of objects using a runtime type.
        /// </summary>
        /// <param name="targetType">The runtime type used to instantiate and map each flattened element.</param>
        /// <param name="targetEntryName">The name of the configuration entry to flatten.</param>
        /// <returns>
        /// An <see cref="IEnumerable"/> containing the deserialized elements created from the specified type.
        /// </returns>
        public IEnumerable FlattenEntryToClassList(Type targetType, string targetEntryName)
        {
            var method = typeof(CfgTreeNode)
                .GetMethod(nameof(FlattenEntryToClassList), new[] { typeof(string) });

            var generic = method.MakeGenericMethod(targetType);
            var result = generic.Invoke(this, new object[] { targetEntryName });

            return (IEnumerable)result;
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

        /// <summary>
        /// Adds a bounded entry (BEGIN ... END) to this node from a class list using the mapper,
        /// dynamically invoking the generic implementation through reflection.
        /// </summary>
        /// <param name="instances">The sequence of instances to serialize into the bounded entry.</param>
        /// <param name="targetType">The runtime type of the class used to map entries.</param>
        /// <param name="parentEntryName">The name of the parent entry (BEGIN marker).</param>
        /// <param name="childEntryName">The name of the repeated child entries within the bounded section.</param>
        /// <param name="variables">
        /// Optional list of variables to include in the parent entry.
        /// If omitted, a count variable is automatically generated.
        /// </param>
        /// <remarks>
        /// This overload relies on reflection to construct a <see cref="List{T}"/> of the specified runtime type
        /// and to invoke the generic <c>AddBoundedEntryFromClassList&lt;T&gt;</c> method dynamically.
        /// It is intended for use cases where the entry type is not known at compile time.
        /// </remarks>
        public void AddBoundedEntryFromClassList(
            IEnumerable instances,
            Type targetType,
            string parentEntryName,
            string childEntryName,
            List<Variable> variables = null
        )
        {
            // Convert IEnumerable to List<T> using reflection
            var listType = typeof(List<>).MakeGenericType(targetType);
            var list = Activator.CreateInstance(listType) as IList;

            foreach (var item in instances)
            {
                list.Add(item);
            }

            // Call the generic method via reflection
            var method = typeof(CfgTreeNode)
                .GetMethod(nameof(AddBoundedEntryFromClassList), new[] {
            typeof(List<>).MakeGenericType(targetType),
            typeof(string),
            typeof(string),
            typeof(List<Variable>)
                });

            var generic = method.MakeGenericMethod(targetType);
            generic.Invoke(this, new object[] { list, parentEntryName, childEntryName, variables });
        }

        #endregion
    }
}