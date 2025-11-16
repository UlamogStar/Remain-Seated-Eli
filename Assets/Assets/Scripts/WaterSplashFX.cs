using UnityEngine;

public class WaterSplashFX : MonoBehaviour
{
    [Header("Water Detection")]
    public string waterTag = "Water";   // Tag on your water trigger

    [Header("Splash Settings")]
    [Tooltip("Different splash sounds to pick from randomly.")]
    public AudioClip[] splashSounds;    // <--- OPTION 1: multiple variations

    [Tooltip("AudioSource used to play splash sounds. If null, a temporary one is created.")]
    public AudioSource audioSource;

    [Tooltip("Minimum horizontal speed to trigger splashes.")]
    public float minSpeedForSplash = 0.2f;

    [Tooltip("Time between splashes while moving in water.")]
    public float splashInterval = 0.4f;

    [Header("Pitch Variation")]
    [Tooltip("Minimum random pitch for splash sounds.")]
    public float minPitch = 0.95f;      // <--- OPTION 2: pitch variation
    [Tooltip("Maximum random pitch for splash sounds.")]
    public float maxPitch = 1.05f;

    [Header("Positioning")]
    [Tooltip("How far down we raycast to find the water surface.")]
    public float raycastDistance = 3f;

    [Tooltip("Offset from the player position for the raycast origin (usually a bit above feet).")]
    public Vector3 raycastOffset = new Vector3(0f, 0.5f, 0f);

    [Header("Splash Particles")]
    public ParticleSystem splashPrefab; // Assign a splash prefab

    private bool inWater;
    private float splashTimer;

    private Rigidbody rb;
    private CharacterController cc;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        cc = GetComponent<CharacterController>();

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void Update()
    {
        if (!inWater)
        {
            splashTimer = 0f;
            return;
        }

        Vector3 velocity = GetVelocity();
        float horizontalSpeed = new Vector3(velocity.x, 0f, velocity.z).magnitude;

        if (horizontalSpeed >= minSpeedForSplash)
        {
            splashTimer -= Time.deltaTime;

            if (splashTimer <= 0f)
            {
                SpawnSplash();
                splashTimer = splashInterval;
            }
        }
        else
        {
            splashTimer = 0f;
        }
    }

    private Vector3 GetVelocity()
    {
        if (rb != null)
            return rb.linearVelocity;

        if (cc != null)
            return cc.velocity;

        return Vector3.zero;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(waterTag))
        {
            inWater = true;
            SpawnSplash(); // optional entry splash
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(waterTag))
        {
            inWater = false;
        }
    }

    private void SpawnSplash()
    {
        Vector3 origin = transform.position + raycastOffset;

        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, raycastDistance))
            return;

        // --- PARTICLES ---
        if (splashPrefab != null)
        {
            ParticleSystem ps = Instantiate(splashPrefab, hit.point, Quaternion.identity);
            ps.Play();

            var main = ps.main;
            float life = main.duration + main.startLifetimeMultiplier;
            Destroy(ps.gameObject, life);
        }

        // --- AUDIO ---
        AudioClip clip = GetRandomSplashClip();
        if (clip == null)
            return;

        float randomPitch = Random.Range(minPitch, maxPitch);

        if (audioSource != null)
        {
            audioSource.pitch = randomPitch;
            audioSource.PlayOneShot(clip);
        }
        else
        {
            // Create a temporary 3D audio source at the splash point
            GameObject tempGO = new GameObject("TempSplashAudio");
            tempGO.transform.position = hit.point;
            AudioSource tempSource = tempGO.AddComponent<AudioSource>();
            tempSource.spatialBlend = 1f; // 3D sound
            tempSource.pitch = randomPitch;
            tempSource.PlayOneShot(clip);

            float duration = clip.length / Mathf.Max(0.01f, randomPitch);
            Destroy(tempGO, duration + 0.1f);
        }
    }

    private AudioClip GetRandomSplashClip()
    {
        if (splashSounds == null || splashSounds.Length == 0)
            return null;

        int index = Random.Range(0, splashSounds.Length);
        return splashSounds[index];
    }
}
