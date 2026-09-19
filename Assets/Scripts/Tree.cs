using System.Collections;
using UnityEngine;

public class Tree : MonoBehaviour
{
    private int currentCut = 0;

    public void Hit()
    {
        StartCoroutine(Shake());
    }

    public IEnumerator Shake()
    {
        Vector3 pos = transform.position;
        transform.position += new Vector3(0.2f, 0f, 0f);
        yield return new WaitForSeconds(0.05f);
        transform.position = pos;
    }
}
