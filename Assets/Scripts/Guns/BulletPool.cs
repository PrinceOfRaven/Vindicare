using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;

public class BulletPool : MonoBehaviour
{
    [SerializeField] private GameObject _bullet;
    [SerializeField] private int _initialCount;

    private readonly Queue<GameObject> _pool = new Queue<GameObject>();
    private Transform _inactiveBulletsPosition;

    private void Awake() 
    {
        _inactiveBulletsPosition = new GameObject("BulletPool").transform;
        _inactiveBulletsPosition.SetParent(transform);
        _inactiveBulletsPosition.gameObject.SetActive(false);

        PreWarm();
    }


    private void PreWarm() 
    {
        for (int i = 0; i < _initialCount; i++) CreateNewBullet();
    }

    public GameObject Get() 
    {
        GameObject bullet = _pool.Count > 0 ? _pool.Dequeue() : CreateNewBullet();
        bullet.SetActive(true);
        return bullet;
    }

    public void Return(GameObject bullet) 
    {
        bullet.SetActive(false);
        bullet.transform.SetParent(_inactiveBulletsPosition);
        _pool.Enqueue(bullet);
    }

    private GameObject CreateNewBullet() 
    {
        GameObject newBullet = Instantiate(_bullet, _inactiveBulletsPosition);
        newBullet.SetActive(false);
        return newBullet;
    }

}
