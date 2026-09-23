using System;
using System.Collections;
using UnityEngine;

namespace MuseoAutista
{
    /// <summary>Ticket dorado coleccionable escondido en el museo. Se recoge mirándolo.</summary>
    public class Ticket : GazeInteractable
    {
        public static event Action Collected;

        bool taken;
        Vector3 basePos;
        float phase;

        void Awake()
        {
            basePos = transform.localPosition;
            phase = UnityEngine.Random.value * 6f;
            dwellTime = 0.9f;
        }

        public override bool IsInteractable => base.IsInteractable && !taken;

        void Update()
        {
            if (taken) return;
            transform.Rotate(0f, 80f * Time.deltaTime, 0f, Space.World);
            transform.localPosition = basePos + Vector3.up * Mathf.Sin(Time.time * 2f + phase) * 0.06f;
        }

        public override void OnGazeActivate()
        {
            if (taken) return;
            taken = true;
            GameState.TicketsCollected++;
            Sfx.Play("ticket", 0.7f);
            Collected?.Invoke();
            StartCoroutine(Fly());
        }

        IEnumerator Fly()
        {
            Vector3 p0 = transform.position;
            Vector3 s0 = transform.localScale;
            for (float t = 0f; t < 0.8f; t += Time.deltaTime)
            {
                float k = t / 0.8f;
                transform.position = p0 + Vector3.up * k * 0.9f;
                transform.Rotate(0f, 720f * Time.deltaTime, 0f, Space.World);
                transform.localScale = s0 * (1f + Mathf.Sin(k * Mathf.PI) * 0.4f) * (1f - k);
                yield return null;
            }
            gameObject.SetActive(false);
        }
    }
}
