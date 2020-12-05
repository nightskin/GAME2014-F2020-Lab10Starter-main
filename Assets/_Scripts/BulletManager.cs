using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BulletManager
{
    // step 1. create a private static instance
    private static BulletManager m_instance = null;

    // step 2. make our default constructor private
    private BulletManager()
    {

    }

    // step 3. make a public static creational method for class access
    public static BulletManager Instance()
    {
        if (m_instance == null)
        {
            m_instance = new BulletManager();
        }
        return m_instance;
    }
    
    public int MaxBullets { get; set; }

    private Queue<GameObject> m_bulletPool;


    /// <summary>
    /// This function initializes the bullet pool with the number of bullets specified and the bullet enumeration type
    /// </summary>
    /// <param name="max_bullets"></param>
    /// <param name="type"></param>
    public void Init(int max_bullets = 50, BulletType type = BulletType.REGULAR)
    {   // step 4 initialize class variables and start the bullet pool build
        MaxBullets = max_bullets;
        _BuildBulletPool(type);
    }

    /// <summary>
    /// This function creates the Object Pool for bullet Game Objects
    /// </summary>
    /// <param name="type"></param>
    private void _BuildBulletPool(BulletType type)
    {
        // create empty Queue structure
        m_bulletPool = new Queue<GameObject>();

        for (int count = 0; count < MaxBullets; count++)
        {
            var tempBullet = BulletFactory.Instance().createBullet(type);
            m_bulletPool.Enqueue(tempBullet);
        }
    }

    public GameObject GetBullet(Vector3 position, Vector3 direction)
    {
        var newBullet = m_bulletPool.Dequeue();
        newBullet.SetActive(true);
        newBullet.transform.position = position;
        newBullet.GetComponent<BulletController>().direction = direction;
        return newBullet;
    }

    public bool HasBullets()
    {
        return m_bulletPool.Count > 0;
    }

    public void ReturnBullet(GameObject returnedBullet)
    {
        returnedBullet.SetActive(false);
        m_bulletPool.Enqueue(returnedBullet);
    }
}
