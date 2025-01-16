using System.Collections;
using System.Linq;
using AntsV2.Components;
using Unity.Entities;
using UnityEngine;

namespace AntsPheromones.Behaviours
{
	public class TestDrawBehaviour : MonoBehaviour
	{
		private EntityManager _entityManager;

		private bool _isCreated;
		#region Event Functions

		private void Start()
		{
			var world = World.DefaultGameObjectInjectionWorld;

			_entityManager = world.EntityManager;

			Debug.Log("Creating entity");

			StartCoroutine(WaitForWorldCreation());
		}


		private void OnDrawGizmos()
		{
			if (!_isCreated) return;


			Entity ent = _entityManager.GetAllEntities().First(entity => _entityManager.HasComponent<ObstacleQuadTree>(entity));

			var quadTree = _entityManager.GetComponentData<ObstacleQuadTree>(ent).Value;

			quadTree.DrawGizmos();

		}

		#endregion

		private IEnumerator WaitForWorldCreation()
		{

			while (!_entityManager.World.IsCreated)
			{
				yield return null;
			}

			Debug.Log("World is created");
			_isCreated = true;
		}
	}
}