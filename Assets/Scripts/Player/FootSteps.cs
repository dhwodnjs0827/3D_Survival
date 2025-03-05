using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FootSteps : MonoBehaviour
{
    public AudioClip[] footStepClips;
    private AudioSource audioSource;
    private Rigidbody _rigidbody;
    public float footStepThreshold;
    public float footStepRate;
    private float footStepTime;

    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (Mathf.Abs(_rigidbody.velocity.y) < 0.1f)
        {
            if (_rigidbody.velocity.magnitude > footStepThreshold)
            {
                if (Time.time - footStepTime > footStepRate)
                {
                    footStepTime = Time.time;
                    audioSource.PlayOneShot(footStepClips[Random.Range(0, footStepClips.Length)]);
                }
            }
        }
    }
}
