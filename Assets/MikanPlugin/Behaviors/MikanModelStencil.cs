using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MikanXR
{
	public class MikanModelStencil : MikanStencil
	{
		private List<MeshFilter> _meshFilters = new List<MeshFilter>();
		private MeshRenderer _meshRenderer;

		public static MikanModelStencil SpawnStencil(
			MikanScene ownerScene, 
			MikanStencilModelInfo modelStencilInfo)
		{
			var stencilPrefab = ownerScene.OwnerManager.OwnerPlugin.stencilPrefab;

			GameObject stencilGO;
			if (stencilPrefab != null)
			{
				stencilGO = Instantiate(stencilPrefab);
			}
			else
			{
				stencilGO = new GameObject();
			}
			stencilGO.name = modelStencilInfo.stencil_name;
			stencilGO.transform.SetParent(ownerScene.transform, false);

			var stencil = stencilGO.GetComponent<MikanModelStencil>();
			if (stencil == null)
			{
				stencil = stencilGO.AddComponent<MikanModelStencil>();
			}

			stencil.Setup(
				ownerScene, 
				modelStencilInfo.stencil_id, 
				modelStencilInfo.stencil_name, 
				modelStencilInfo.relative_transform);

			return stencil;
		}

		protected override void Setup(
			MikanScene ownerScene, 
			int stencilId, 
			string stencilName, 
			MikanTransform relativeTransform)
		{
			base.Setup(ownerScene, stencilId, stencilName, relativeTransform);

			// Put all stencils on the UI layer so they don't render in the XR camera
			// TODO: Make this configurable on the settings
			gameObject.layer = LayerMask.NameToLayer("UI");

			// Create a new mesh renderer
			_meshRenderer = gameObject.AddComponent<MeshRenderer>();
			// TODO: Make the shadow options configurable on the settings
			_meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
			_meshRenderer.receiveShadows = false;

			// Use plugin defined material if given
			_meshRenderer.material = ownerScene.OwnerManager.OwnerPlugin.modelStencilMaterial;
			if (_meshRenderer.material == null)
			{
				_meshRenderer.material = new Material(Shader.Find("Standard"));
			}
		}

		protected override void Teardown()
		{
			base.Teardown();

			// Free all mesh filters
			UpdateMeshAllocation(0);
		}

		public void ApplyModelStencilInfo(MikanStencilModelInfo mikanStencilModel)
		{
			// Update the stencil transform from the event
			_mikanSpaceTransform.ApplyMikanTransform(mikanStencilModel.relative_transform);

			UpdateSceneTransform();
		}

		public void ApplyModelRenderGeometry(MikanStencilModelRenderGeometry modelGeo)
		{
			UpdateMeshAllocation(modelGeo.meshes.Count);

			for (int meshIndex = 0; meshIndex < modelGeo.meshes.Count; meshIndex++)
			{
				MikanTriagulatedMesh mikanMeshData = modelGeo.meshes[meshIndex];

				MeshFilter meshFilter = _meshFilters[meshIndex];
				Mesh mesh = meshFilter.mesh;

				ApplyDataToMesh(mikanMeshData, mesh);
			}
		}

		private void UpdateMeshAllocation(int desiredMeshCount)
		{
			while (_meshFilters.Count < desiredMeshCount)
			{
				var meshFilter = gameObject.AddComponent<MeshFilter>();
				meshFilter.mesh = new Mesh();

				_meshFilters.Add(meshFilter);
			}

			while (_meshFilters.Count > desiredMeshCount)
			{
				var meshFilter = _meshFilters[_meshFilters.Count - 1];
				_meshFilters.RemoveAt(_meshFilters.Count - 1);

				Destroy(meshFilter);
			}
		}

		private void ApplyDataToMesh(MikanTriagulatedMesh mikanMeshData, Mesh mesh)
		{
			mesh.Clear();

			int vertexCount = mikanMeshData.vertices.Count;
			var vertices = new Vector3[vertexCount];
			for (int vertexIndex = 0; vertexIndex < vertexCount; vertexIndex++)
			{
				vertices[vertexIndex] = MikanMath.MikanVector3fToVector3(mikanMeshData.vertices[vertexIndex]);
			}

			int normalCount = mikanMeshData.normals.Count;
			var normals = new Vector3[normalCount];
			for (int normalIndex = 0; normalIndex < normalCount; normalIndex++)
			{
				normals[normalIndex] = MikanMath.MikanVector3fToVector3(mikanMeshData.normals[normalIndex]);
			}

			int uvCount = mikanMeshData.texels.Count;
			var uv = new Vector2[uvCount];
			for (int texelIndex = 0; texelIndex < uvCount; texelIndex++)
			{
				uv[texelIndex] = MikanMath.MikanVector2fToVector2(mikanMeshData.texels[texelIndex]);
			}

			int indexCount = mikanMeshData.indices.Count;
			var indices = new int[indexCount];
			for (int indexIndex = 0; indexIndex < indexCount; indexIndex+=3)
			{
				// Flip the winding order of the triangles
				indices[indexIndex+2] = mikanMeshData.indices[indexIndex+0];
				indices[indexIndex+1] = mikanMeshData.indices[indexIndex+1];
				indices[indexIndex+0] = mikanMeshData.indices[indexIndex+2];
			}

			mesh.vertices = vertices;
			mesh.normals = normals;
			mesh.uv = uv;
			mesh.triangles = indices;
			if (normalCount == 0)
			{
				mesh.RecalculateNormals();
			}

			mesh.RecalculateBounds();
			mesh.UploadMeshData(true);
		}
	}
}
