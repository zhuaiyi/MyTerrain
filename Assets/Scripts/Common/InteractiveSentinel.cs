using UnityEngine;

public class InteractiveSentinel : MonoBehaviour
{
	private bool reallyEnabled;

	void OnEnable()
	{
		if (!reallyEnabled)
		{
			reallyEnabled = true;
		}
		else
		{
			//World.DefaultGameObjectInjectionWorld.GetExistingSystem<SpawnerSystem>().SpawnAdditionalUnits();

			CameraController c = FindObjectOfType<CameraController>();

			Plane ground = new Plane(Vector3.up, 0);
			Ray r = new Ray(c.transform.position, c.transform.forward);

			float t;
			if (ground.Raycast(r, out t))
			{
				Vector3 p = r.origin + t * r.direction;

				c.anchorPosition = p;
				c.anchorDirection = -r.direction;
				c.zoom = t;
			}
		}
	}
}
