using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StudioElevenLib.Level5.Binary.Logic
{
    /// <summary>
    /// TreeNode class for representing hierarchical data structures.
    /// </summary>
    /// <typeparam name="T">The type of item stored in the node</typeparam>
    public class TreeNode<T>
    {
        public T Item { get; set; }
        public List<TreeNode<T>> Children { get; set; }
        public int Level { get; set; }
        public TreeNode<T> Parent { get; set; }

        /// <summary>
        /// Constructor to initialize a node with an item and a level.
        /// </summary>
        /// <param name="item">The item of the node</param>
        /// <param name="level">The level of the node (default is 0)</param>
        public TreeNode(T item, int level = 0)
        {
            Item = item;
            Level = level;
            Children = new List<TreeNode<T>>();
            Parent = null;
        }

        /// <summary>
        /// Adds a child node to the current node.
        /// </summary>
        /// <param name="child">The child node to add</param>
        public void AddChild(TreeNode<T> child)
        {
            child.Level = Level + 1;
            child.Parent = this;
            Children.Add(child);
        }

        /// <summary>
        /// Method to get a string representation of the tree structure of the node and its children.
        /// </summary>
        /// <param name="level">The indentation level for displaying the tree</param>
        /// <returns>A string representing the tree structure</returns>
        public string PrintTree(int level = 0)
        {
            string ret = new string('\t', level) + $"- {Item} (Level: {Level})\n";

            foreach (var child in Children)
            {
                ret += child.PrintTree(level + 1); // Increase the level for the tree structure
            }

            return ret;
        }

        /// <summary>
        /// Recursively searches for a node based on its level.
        /// </summary>
        /// <param name="targetLevel">The level to search for</param>
        /// <returns>The node corresponding to the target level, or null if not found</returns>
        public TreeNode<T> GetTreeNodeByLevel(int targetLevel)
        {
            if (Level == targetLevel)
            {
                return this;
            }

            // Search among the children
            foreach (var child in Children)
            {
                var result = child.GetTreeNodeByLevel(targetLevel);
                if (result != null)
                {
                    return result;
                }
            }

            // If the node is not found, return null
            return null;
        }

        /// <summary>
        /// Retrieves a node at a lower level, if possible.
        /// </summary>
        /// <returns>The node at the lower level or the root node if already at level 0</returns>
        public TreeNode<T> GetLowerLevelNode()
        {
            return Level > 0 ? GetTreeNodeByLevel(Level - 1) : this;
        }
    }
}