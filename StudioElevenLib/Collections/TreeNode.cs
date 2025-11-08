using System.Collections.Generic;

namespace StudioElevenLib.Collections
{
    /// <summary>
    /// A tree node class for managing hierarchical data structures.
    /// </summary>
    /// <typeparam name="T">The type of item stored in the node</typeparam>
    public class TreeNode<T>
    {
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

        public T Item { get; set; }
        public List<TreeNode<T>> Children { get; set; }
        public int Level { get; set; }
        public TreeNode<T> Parent { get; set; }

        /// <summary>
        /// Adds a child node to the current node.
        /// </summary>
        /// <param name="child">The child node to add</param>
        public void AddChild(TreeNode<T> child)
        {
            child.Parent = this;
            Children.Add(child);
            UpdateChildLevels(child, Level + 1);
        }

        private void UpdateChildLevels(TreeNode<T> node, int newLevel)
        {
            node.Level = newLevel;

            // Mettre à jour récursivement tous les enfants
            foreach (var child in node.Children)
            {
                UpdateChildLevels(child, newLevel + 1);
            }
        }

        /// <summary>
        /// Method to get a string representation of the tree structure of the node and its children.
        /// </summary>
        /// <param name="level">The indentation level for displaying the tree</param>
        /// <returns>A string representing the tree structure</returns>
        public string PrintTree(int level = 0)
        {
            string ret = new string('\t', level) + string.Format("- {0} (Level: {1})\n", Item, Level);
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

        /// <summary>
        /// Removes a child node from the current node.
        /// </summary>
        /// <param name="child">The child node to remove</param>
        /// <returns>True if the child was successfully removed, false otherwise</returns>
        public bool RemoveChild(TreeNode<T> child)
        {
            if (Children.Remove(child))
            {
                child.Parent = null;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes this node from its parent.
        /// </summary>
        /// <returns>True if successfully removed from parent, false if node has no parent</returns>
        public bool RemoveFromParent()
        {
            if (Parent != null)
            {
                return Parent.RemoveChild(this);
            }
            return false;
        }

        /// <summary>
        /// Gets the root node of the tree.
        /// </summary>
        /// <returns>The root node</returns>
        public TreeNode<T> GetRoot()
        {
            TreeNode<T> current = this;
            while (current.Parent != null)
            {
                current = current.Parent;
            }
            return current;
        }

        /// <summary>
        /// Searches for a node containing a specific item using depth-first search.
        /// </summary>
        /// <param name="item">The item to search for</param>
        /// <returns>The first node containing the item, or null if not found</returns>
        public TreeNode<T> FindNode(T item)
        {
            if (EqualityComparer<T>.Default.Equals(Item, item))
            {
                return this;
            }

            foreach (var child in Children)
            {
                var result = child.FindNode(item);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        /// <summary>
        /// Searches for nodes that match a predicate condition.
        /// </summary>
        /// <param name="predicate">The condition to match</param>
        /// <returns>A list of matching nodes</returns>
        public List<TreeNode<T>> FindNodes(System.Func<T, bool> predicate)
        {
            var results = new List<TreeNode<T>>();
            FindNodesRecursive(predicate, results);
            return results;
        }

        private void FindNodesRecursive(System.Func<T, bool> predicate, List<TreeNode<T>> results)
        {
            if (predicate(Item))
            {
                results.Add(this);
            }

            foreach (var child in Children)
            {
                child.FindNodesRecursive(predicate, results);
            }
        }

        /// <summary>
        /// Gets all leaf nodes (nodes with no children) in the subtree.
        /// </summary>
        /// <returns>A list of leaf nodes</returns>
        public List<TreeNode<T>> GetLeafNodes()
        {
            var leafNodes = new List<TreeNode<T>>();
            GetLeafNodesRecursive(leafNodes);
            return leafNodes;
        }

        private void GetLeafNodesRecursive(List<TreeNode<T>> leafNodes)
        {
            if (Children.Count == 0)
            {
                leafNodes.Add(this);
            }
            else
            {
                foreach (var child in Children)
                {
                    child.GetLeafNodesRecursive(leafNodes);
                }
            }
        }

        /// <summary>
        /// Gets all nodes at a specific level in the subtree.
        /// </summary>
        /// <param name="targetLevel">The target level</param>
        /// <returns>A list of nodes at the specified level</returns>
        public List<TreeNode<T>> GetNodesAtLevel(int targetLevel)
        {
            var nodes = new List<TreeNode<T>>();
            GetNodesAtLevelRecursive(targetLevel, nodes);
            return nodes;
        }

        private void GetNodesAtLevelRecursive(int targetLevel, List<TreeNode<T>> nodes)
        {
            if (Level == targetLevel)
            {
                nodes.Add(this);
            }
            else if (Level < targetLevel)
            {
                foreach (var child in Children)
                {
                    child.GetNodesAtLevelRecursive(targetLevel, nodes);
                }
            }
        }

        /// <summary>
        /// Gets the path from the root to this node.
        /// </summary>
        /// <returns>A list of nodes representing the path from root to this node</returns>
        public List<TreeNode<T>> GetPathFromRoot()
        {
            var path = new List<TreeNode<T>>();
            TreeNode<T> current = this;

            while (current != null)
            {
                path.Insert(0, current);
                current = current.Parent;
            }

            return path;
        }

        /// <summary>
        /// Gets the depth of the subtree (maximum levels below this node).
        /// </summary>
        /// <returns>The depth of the subtree</returns>
        public int GetDepth()
        {
            if (Children.Count == 0)
            {
                return 0;
            }

            int maxChildDepth = 0;
            foreach (var child in Children)
            {
                int childDepth = child.GetDepth();
                if (childDepth > maxChildDepth)
                {
                    maxChildDepth = childDepth;
                }
            }

            return maxChildDepth + 1;
        }

        /// <summary>
        /// Gets the total number of nodes in the subtree (including this node).
        /// </summary>
        /// <returns>The total count of nodes</returns>
        public int GetNodeCount()
        {
            int count = 1; // Count this node
            foreach (var child in Children)
            {
                count += child.GetNodeCount();
            }
            return count;
        }

        /// <summary>
        /// Checks if this node is an ancestor of the specified node.
        /// </summary>
        /// <param name="node">The node to check</param>
        /// <returns>True if this node is an ancestor of the specified node</returns>
        public bool IsAncestorOf(TreeNode<T> node)
        {
            if (node == null) return false;

            TreeNode<T> current = node.Parent;
            while (current != null)
            {
                if (current == this)
                {
                    return true;
                }
                current = current.Parent;
            }
            return false;
        }

        /// <summary>
        /// Checks if this node is a descendant of the specified node.
        /// </summary>
        /// <param name="node">The node to check</param>
        /// <returns>True if this node is a descendant of the specified node</returns>
        public bool IsDescendantOf(TreeNode<T> node)
        {
            if (node == null) return false;
            return node.IsAncestorOf(this);
        }

        /// <summary>
        /// Checks if this node is a leaf node (has no children).
        /// </summary>
        /// <returns>True if this is a leaf node</returns>
        public bool IsLeaf()
        {
            return Children.Count == 0;
        }

        /// <summary>
        /// Checks if this node is the root node (has no parent).
        /// </summary>
        /// <returns>True if this is the root node</returns>
        public bool IsRoot()
        {
            return Parent == null;
        }

        /// <summary>
        /// Gets the sibling nodes (nodes with the same parent).
        /// </summary>
        /// <returns>A list of sibling nodes (excluding this node)</returns>
        public List<TreeNode<T>> GetSiblings()
        {
            if (Parent == null)
            {
                return new List<TreeNode<T>>();
            }

            var siblings = new List<TreeNode<T>>();
            foreach (var sibling in Parent.Children)
            {
                if (sibling != this)
                {
                    siblings.Add(sibling);
                }
            }
            return siblings;
        }

        /// <summary>
        /// Moves this node to become a child of another node.
        /// </summary>
        /// <param name="newParent">The new parent node</param>
        public void MoveTo(TreeNode<T> newParent)
        {
            if (newParent == null)
                throw new System.ArgumentNullException(nameof(newParent));

            if (newParent.IsDescendantOf(this))
                throw new System.InvalidOperationException("Cannot move node to its own descendant");

            RemoveFromParent();
            newParent.AddChild(this);
        }

        /// <summary>
        /// Clones this node and its entire subtree.
        /// </summary>
        /// <returns>A deep copy of this node and all its descendants</returns>
        public TreeNode<T> Clone()
        {
            var clone = new TreeNode<T>(Item, Level);

            foreach (var child in Children)
            {
                clone.AddChild(child.Clone());
            }

            return clone;
        }

        /// <summary>
        /// Performs a depth-first traversal and executes an action on each node.
        /// </summary>
        /// <param name="action">The action to perform on each node</param>
        public void Traverse(System.Action<TreeNode<T>> action)
        {
            action(this);
            foreach (var child in Children)
            {
                child.Traverse(action);
            }
        }

        /// <summary>
        /// Performs a breadth-first traversal and executes an action on each node.
        /// </summary>
        /// <param name="action">The action to perform on each node</param>
        public void TraverseBreadthFirst(System.Action<TreeNode<T>> action)
        {
            var queue = new Queue<TreeNode<T>>();
            queue.Enqueue(this);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                action(current);

                foreach (var child in current.Children)
                {
                    queue.Enqueue(child);
                }
            }
        }
    }
}