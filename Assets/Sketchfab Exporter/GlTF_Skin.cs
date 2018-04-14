﻿#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GlTF_Skin : GlTF_Writer {
	public GlTF_Matrix bindShapeMatrix;
	public Matrix4x4[] invBindMatrices;
	public int invBindMatricesAccessorIndex;
	public Transform node;
	public string[] jointNames;
	public Transform mesh;

	public GlTF_Skin() { }

	public static string GetNameFromObject(Object o)
	{
		return "skin_" + GlTF_Writer.GetNameFromObject(o, true);
	}

	public void setBindShapeMatrix(Transform mesh)
	{
		Matrix4x4 mat = Matrix4x4.identity;
		bindShapeMatrix = new GlTF_Matrix(mat);
		bindShapeMatrix.name = "bindShapeMatrix";
	}

	public void Populate (Transform m, ref GlTF_Accessor invBindMatricesAccessor, int invBindAccessorIndex)
	{
		SkinnedMeshRenderer skinMesh = m.GetComponent<SkinnedMeshRenderer>();
		if (!skinMesh)
			return;

		// Populate bind poses. From https://docs.unity3d.com/ScriptReference/Mesh-bindposes.html:
		// The bind pose is bone's inverse transformation matrix
		// In this case we also make this matrix relative to the root
		// So that we can move the root game object around freely
		Mesh mesh = skinMesh.sharedMesh;
		Matrix4x4[] invBindMatrices = new Matrix4x4[skinMesh.sharedMesh.bindposes.Length];

		for(int i=0;i<invBindMatrices.Length;++i)
		{
			// Generates inverseWorldMatrix in right-handed coordinate system
			// Manually converts world translation and rotation from left to right handed coordinates systems
			Vector3 pos = skinMesh.bones[i].position;
			Quaternion rot = skinMesh.bones[i].rotation;
			convertQuatLeftToRightHandedness(ref rot);
			convertVector3LeftToRightHandedness(ref pos);

			invBindMatrices[i] = Matrix4x4.TRS(pos, rot, skinMesh.bones[i].lossyScale).inverse * sceneRootMatrix.inverse;
		}

		invBindMatricesAccessor.Populate(invBindMatrices, m);
		invBindMatricesAccessorIndex = invBindAccessorIndex;

		// Fill jointNames
		jointNames = new string[skinMesh.bones.Length];
		for(int i=0; i< skinMesh.bones.Length; ++i)
		{
			jointNames[i] = GlTF_Node.GetNameFromObject(skinMesh.bones[i]);
		}
	}

	public static List<string> findRootSkeletons(SkinnedMeshRenderer skin)
	{
		List<string> skeletons = new List<string>();
		List<Transform> tbones = new List<Transform>();
		// Get bones
		foreach (Transform bone in skin.bones)
		{
			tbones.Add(bone);
		}
		List<Transform> haveBParents = new List<Transform>();
		// Check and list bones that have parents that are bon in this skin
		foreach (Transform b in tbones)
		{
			Transform temp = b.parent;
			while (temp.parent)
			{
				if (tbones.Contains(temp))
				{
					haveBParents.Add(b);
					break;
				}
				temp = temp.parent;
			}
		}

		// Remove bones having parents from the list
		foreach (Transform b in haveBParents)
		{
			tbones.Remove(b);
		}
		foreach (Transform t in tbones)
			skeletons.Add(GlTF_Node.GetNameFromObject(t));

		return skeletons;
	}

	public override void Write ()
	{
		Indent();	jsonWriter.Write ("{\n");
		IndentIn();

		if (bindShapeMatrix != null)
		{
			CommaNL();
			bindShapeMatrix.Write();
		}

		Indent(); jsonWriter.Write(",\n");
		Indent(); jsonWriter.Write("\"inverseBindMatrices\": "+ invBindMatricesAccessorIndex + ",\n");
		Indent(); jsonWriter.Write ("\"jointNames\": [\n");

		IndentIn();
		foreach (string j in jointNames)
		{
			CommaNL();
			Indent();	jsonWriter.Write ("\""+ j + "\"");
		}
		IndentOut();
		jsonWriter.WriteLine();
		Indent(); jsonWriter.Write ("],\n");
		Indent(); jsonWriter.Write("\"name\": \"" + name + "\"\n");
		IndentOut();
		Indent();	jsonWriter.Write ("}");
	}
}
#endif