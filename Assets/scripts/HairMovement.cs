using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class HairMovement : MonoBehaviour {
    
    private const string prefabPath = "prefabs/HairRender";

    [SerializeField] private int hairSize = 4;

    [SerializeField] private float hairYOffset = .5f;
    [SerializeField] private float hairXOffset = .45f;
    [SerializeField] private float hairDelay = .01f;

    private float baseLayer = 0;
    private float behindLayer = 1;

    private Transform target;
    private Controller2D controller;

    private List<GameObject> rightChildren = new List<GameObject>();
    private List<GameObject> leftChildren = new List<GameObject>();

    private float xSmoothing;

    private void Start() {

        baseLayer = transform.position.z;
        behindLayer += transform.position.z;

        /* find our player! */
        GameObject player = GameObject.FindWithTag("Player");
        target = player.transform;
        controller = player.GetComponent<Controller2D>();

        /* manage hair */
        GameObject hairPrefab = Resources.Load<GameObject>(prefabPath);
        for (int i = 0; i < hairSize; ++i) {
            leftChildren.Add(instantiateHair(hairPrefab, i, baseLayer));
            rightChildren.Add(instantiateHair(hairPrefab, i, behindLayer));
        }
    }

    /* execute after player movement */
    private void LateUpdate() {
        /* update both left and right hair */
        ThreadStart leftProcessing = delegate { moveChildren(leftChildren, -1); };
        ThreadStart rightProcessing = delegate { moveChildren(rightChildren, 1); };
        
        leftProcessing.Invoke();
        rightProcessing.Invoke();
    }

    /**
     * <summary>
     * Move all components according to a tracking position.
     * </summary>
     * */
    private void moveChildren(List<GameObject> children, int dir) {
        /* get last tracking position */
        Vector2 trackPos = new Vector2(target.position.x + dir * controller.collisions.faceDirection * hairXOffset, 
            target.position.y + hairYOffset);

        /* iterate */
        for (int i = 0; i < children.Count; ++i) {
            Vector3 mod = children[i].transform.position;

            /* move smoothly~ */
            mod.x += (trackPos.x - mod.x) / 2f;
            mod.y += (trackPos.y - .15f - mod.y) / 2f;

            mod.x = Mathf.SmoothDamp(children[i].transform.position.x, mod.x, ref xSmoothing, hairDelay);

            /* move child */
            children[i].transform.position = mod;
            trackPos = mod;
        }
    }

    /**
     * <summary>
     * Instantiate a hair component
     * </summary>
     * */
    private GameObject instantiateHair(GameObject hairPrefab, int index, float layer) {
        GameObject hairComp = Instantiate(hairPrefab);

        /* Set our properties */
        hairComp.transform.SetParent(transform);
        hairComp.transform.position = target.position + new Vector3(hairXOffset, hairYOffset, layer);
        hairComp.transform.localScale *= Mathf.Max(1, Mathf.Min(2, 3 - index));

        return hairComp;
    }
}