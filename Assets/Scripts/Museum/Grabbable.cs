using System;
using System.Collections;
using UnityEngine;

namespace MuseoAutista
{
    /// <summary>
    /// Pieza que se agarra con la mirada. Mientras se sostiene flota delante de la
    /// cámara; se coloca mirando un PlacementSocket. Si se agarra otra pieza, la
    /// anterior vuelve a su lugar de origen.
    /// </summary>
    public class Grabbable : GazeInteractable
    {
        public string itemId;
        public string displayName;
        [Tooltip("Nombre de la sala donde va esta pieza (para mensajes de ayuda).")]
        public string hallName;

        public static Grabbable Held { get; private set; }
        public static event Action<Grabbable> PickedUp;

        public bool IsPlaced { get; private set; }

        Vector3 homePosition;
        Quaternion homeRotation;
        Collider[] colliders;
        Coroutine moving;

        void Awake()
        {
            homePosition = transform.position;
            homeRotation = transform.rotation;
            colliders = GetComponentsInChildren<Collider>();
        }

        void OnDestroy()
        {
            if (Held == this) Held = null;
        }

        public override bool IsInteractable => base.IsInteractable && Held != this && !IsPlaced;

        public override void OnGazeActivate()
        {
            if (Held != null && Held != this) Held.ReturnHome();
            if (moving != null) StopCoroutine(moving);
            Held = this;
            SetColliders(false);
            Sfx.Play("grab");
            PickedUp?.Invoke(this);
        }

        void LateUpdate()
        {
            if (Held != this || PlayerRig.Instance == null || PlayerRig.Instance.holdPoint == null) return;
            Transform hp = PlayerRig.Instance.holdPoint;
            float k = 1f - Mathf.Exp(-14f * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, hp.position, k);
            Quaternion spin = Quaternion.AngleAxis(Time.time * 35f, Vector3.up);
            Quaternion target = Quaternion.LookRotation(Vector3.ProjectOnPlane(hp.forward, Vector3.up).normalized + Vector3.up * 0.001f) * spin;
            transform.rotation = Quaternion.Slerp(transform.rotation, target, k);
        }

        public void PlaceAt(Transform anchor)
        {
            if (Held == this) Held = null;
            IsPlaced = true;
            moving = StartCoroutine(MoveTo(anchor.position, anchor.rotation, false));
        }

        public void ReturnHome()
        {
            if (Held == this) Held = null;
            moving = StartCoroutine(MoveTo(homePosition, homeRotation, true));
        }

        IEnumerator MoveTo(Vector3 pos, Quaternion rot, bool enableColliders)
        {
            Vector3 p0 = transform.position;
            Quaternion r0 = transform.rotation;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.45f;
                float e = Mathf.SmoothStep(0f, 1f, t);
                transform.position = Vector3.Lerp(p0, pos, e) + Vector3.up * Mathf.Sin(e * Mathf.PI) * 0.15f;
                transform.rotation = Quaternion.Slerp(r0, rot, e);
                yield return null;
            }
            transform.SetPositionAndRotation(pos, rot);
            if (enableColliders) SetColliders(true);
            moving = null;
        }

        void SetColliders(bool on)
        {
            foreach (var c in colliders) if (c != null) c.enabled = on;
        }
    }
}
