(* ::Package:: *)

BeginPackage["RhinoUtilities`", {"NETLink`"}]


LoadNETType["Rhino.RhinoDoc"];
LoadNETType["Wolfram.Rhino.WolframScriptingPlugIn"];


FromRhinoPoint3d::usage = ""


ToRhinoPoint3d::usage = ""


RhinoDocObjects::usage = "RhinoDocObjects[doc] returns a list of the RhinoObjects in the document.";


RhinoDocInformation::usage = "RhinoDocInformation[doc] returns a summary of information about 'doc'.";


RhinoShow::usage = "";


FromRhinoMesh::usage = "";


ToRhinoMesh::usave = "";


RhinoMeshUnion::usage = "";


RhinoMeshIntersection::usage = "";


RhinoMeshDifference::usage = "";


RhinoMeshSplit::usage = "";


Begin["`Private`"]


FromRhinoPoint3d[pt_]:=
	{pt@X, pt@Y, pt@Z}


FromRhinoPoint3d[pts_List]:=
	FromRhinoPoint3d /@ pts


ToRhinoPoint3d[{x_?NumberQ,y_?NumberQ,z_?NumberQ}]:=
	NETNew["Rhino.Geometry.Point3d",x,y,z]


ToRhinoPoint3d[pts:{{_?NumberQ,_?NumberQ,_?NumberQ}...}]:=
	ToRhinoPoint3d /@ pts


RhinoDocObjects[doc_:RhinoDoc`ActiveDoc]:=
	With[{it = doc@Objects@GetEnumerator[]},
		Reap[While[it@MoveNext[], Sow[it@Current]]][[2,1]]
	]


RhinoDocInformation[doc_:RhinoDoc`ActiveDoc]:=
	Block[{objs,i},
		objs=RhinoDocObjects[doc];
		Column[{
			"Document: "<>doc@Name,
			Row[{"Object count: ", Length[objs]}],
			"",
			i=0;
			Grid[Join[
				{{"","Sel","Type","Full Type"}},
				{
					i++,
					(* This is not working because Views, Redraw, and IsSelected are in the wrong context *)
					DynamicModule[{isSelected=#@IsSelected[False]=!=0},
						Checkbox[Dynamic[isSelected, 
							(#@Select[!isSelected];doc@Views@Redraw[]; isSelected=#)&]]
					],
					#@ObjectType@ToString[],
					#@ToString[]
				}& /@ objs
			], Alignment->Left]
		}]
	]


SetAttributes[RhinoShow, Listable];
RhinoShow[obj_]:=
	Block[{doc=RhinoDoc`ActiveDoc},
		Switch[obj@ToString[],
			"Rhino.Geometry.Mesh",
				doc@Objects@AddMesh[obj],
			_,
				MessageDialog[Row[{"Unknown Rhino object type ",obj@ToString[]}]]
		];
		doc@Views@Redraw[]
	]


SetAttributes[FromRhinoMesh, Listable];
FromRhinoMesh[mesh_]:=
	MeshRegion[
		Wolfram`Rhino`WolframScriptingPlugIn`RhinoMeshVertices[mesh],
		Polygon[Wolfram`Rhino`WolframScriptingPlugIn`RhinoMeshFaces[mesh]]
	]


ToRhinoMesh[meshRegion_]:=
	Wolfram`Rhino`WolframScriptingPlugIn`ToRhinoMesh[
		MeshCoordinates[meshRegion],
		(First [#]-1)&/@ MeshCells[meshRegion,2]
	]


ToRhinoMesh[vertices:{{_?NumericQ, _?NumericQ, _?NumericQ}..}, faces:{{_Integer..}..}] :=
	Wolfram`Rhino`WolframScriptingPlugIn`ToRhinoMesh[vertices, faces]


RhinoMeshUnion[meshes_]:=
	Rhino`Geometry`Mesh`CreateBooleanUnion[
		MakeNETObject[meshes,"Rhino.Geometry.Mesh[]"]
	]


RhinoMeshDifference[meshes1_,meshes2_]:=
	Rhino`Geometry`Mesh`CreateBooleanDifference[
		MakeNETObject[meshes1,"Rhino.Geometry.Mesh[]"],
		MakeNETObject[meshes2,"Rhino.Geometry.Mesh[]"]
	]


RhinoMeshIntersection[meshes1_,meshes2_]:=
	Rhino`Geometry`Mesh`CreateBooleanIntersection[
		MakeNETObject[meshes1,"Rhino.Geometry.Mesh[]"],
		MakeNETObject[meshes2,"Rhino.Geometry.Mesh[]"]
	]


RhinoMeshSplit[meshes1_,meshes2_]:=
	Rhino`Geometry`Mesh`CreateBooleanSplit[
		MakeNETObject[meshes1,"Rhino.Geometry.Mesh[]"],
		MakeNETObject[meshes2,"Rhino.Geometry.Mesh[]"]
	]


End[]


EndPackage[]
