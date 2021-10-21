using System;
using System.Collections.Generic;

/// <summary>
/// LRU简单实现
/// </summary>
/// <typeparam name="T"></typeparam>
public class LRUCache<T>
{
    // key -> Node(key, val)
    private Dictionary<int, Node<T>> map;
    // Node(k1, v1) <-> Node(k2, v2)...
    private DoubleLink<T> cache;
    // 最大容量
    private int cap;

    public event Action<int> OnCacheRemove;

    public LRUCache(int capacity)
    {
        this.cap = capacity;
        map = new Dictionary<int, Node<T>>();
        cache = new DoubleLink<T>();
    }

    /// <summary>
    /// 访问O(1)
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public T Get(int key)
    {
        if (!map.ContainsKey(key))
            return default(T);
        T val = map[key].val;
        // 利用 put 方法把该数据提前
        Put(key, val);
        return val;
    }

    /// <summary>
    /// 放入缓存队列
    /// O(1)
    /// </summary>
    /// <param name="key"></param>
    /// <param name="val"></param>
    public void Put(int key, T val)
    {
        // 先把新节点 node 做出来
        Node<T> node = new Node<T>(key, val);
        if (map.ContainsKey(key))
        {
            // 删除旧的节点，新的插到头部
            cache.Remove(map[key]);
            cache.AddFirst(node);
            // 更新 map 中对应的数据
            map.Add(key, node);
        }
        else
        {
            if (cap == cache.Size)
            {
                // 删除链表最后一个数据
                Node<T> last = cache.RemoveLast();
                map.Remove(last.key);
                if (OnCacheRemove != null)
                    OnCacheRemove(last.key);
            }
            // 直接添加到头部
            cache.AddFirst(node);
            map.Add(key, node);
        }
    }
}

/// <summary>
/// 双向链表
/// </summary>
/// <typeparam name="T"></typeparam>
public class DoubleLink<T>
{
    //表头
    private readonly Node<T> linkHead;
    //节点个数
    private int size;
    public int Size
    {
        get { return this.size; }
    }

    //判空
    public bool IsEmpty
    {
        get { return this.size == 0; }
    }

    public DoubleLink()
    {
        linkHead = new Node<T>(0, default(T));
        linkHead.prev = linkHead;
        linkHead.next = linkHead;
        size = 0;
    }

    /// <summary>
    /// 添加到表头 O(1)
    /// </summary>
    /// <param name="node"></param>
    public void AddFirst(Node<T> node)
    {
        node.prev = linkHead;
        node.next = linkHead.next;
        linkHead.next.prev = node;
        linkHead.next = node;
        size++;
    }

    /// <summary>
    /// 移除中间某个节点 O(1)
    /// </summary>
    /// <param name="node"></param>
    public void Remove(Node<T> node)
    {
        node.prev.next = node.next;
        node.next.prev = node.prev;
        size--;
    }

    /// <summary>
    /// 从尾部移除 O(1)
    /// </summary>
    /// <returns></returns>
    public Node<T> RemoveLast()
    {
        Node<T> deNode = linkHead.prev;
        Remove(deNode);
        size--;
        return deNode;
    }
}

/// <summary>
/// 双向链表节点
/// </summary>
/// <typeparam name="T"></typeparam>
public class Node<T>
{
    public int key;
    public T val;
    public Node<T> next;
    public Node<T> prev;
    public Node(int k, T v)
    {
        this.key = k;
        this.val = v;
    }
}