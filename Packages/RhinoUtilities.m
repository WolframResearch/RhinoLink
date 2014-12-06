(* ::Package:: *)

BeginPackage["RhinoUtilities`", {"NETLink`"}]


LoadNETType["Rhino.RhinoDoc"];
LoadNETType["Wolfram.Rhino.WolframScriptingPlugIn"];


(* Agenda
	- FromRhino[...,"Point3d[]"]
	- mesh conversions
- nurbs curve coversions
*)


(* 
 * Conversions
 *)


FromRhino::usage = "FromRhino[obj] attempts to convert a Rhino object to an equivalent Wolfram Language expression. 
FromRhino[obj, type] attempts to convert a Rhino object of the specified type to an equivalent Wolfram Language expression.";


ToRhino::usage = "ToRhino[expr] attempts to convert a Wolfram Language expression to an equivalent Rhino object.";


(* 
 * Queries
 *)


RhinoDocObjects::usage = "RhinoDocObjects[doc] returns a list of the RhinoObjects in the document.";


RhinoDocInformation::usage = "RhinoDocInformation[doc] returns a summary of information about 'doc'.";


(* 
 * Rhino Geometric Operations 
 *)


RhinoMeshUnion::usage = "";


RhinoMeshIntersection::usage = "";


RhinoMeshDifference::usage = "";


RhinoMeshSplit::usage = "";


(* 
 * Other Rhino Operations 
 *)


RhinoShow::usage = "";


(* 
 *  
 *)


Begin["`Private`"]


(* 
 * Conversions
 *)


FromRhino[obj_] :=
	FromRhino[obj, obj@GetType[]@ToString[]]


FromRhino[_, type_] :=
	$Failed[type]


(* Rhino.Geometry.Point3d *)


ToRhino[{x_?NumericQ, y_?NumericQ, z_?NumericQ}] :=
	NETNew["Rhino.Geometry.Point3d", N[x], N[y], N[z]]


FromRhino[obj_, "Rhino.Geometry.Point3d"] :=
	{obj@X, obj@Y, obj@Z}


(* Rhino.Geometry.Point3d[] *)


ToRhino[obj:{{_?NumericQ, _?NumericQ, _?NumericQ}...}] :=
	ReturnAsNETObject@WolframScriptingPlugIn`ToRhinoPoint3dArray[obj]


FromRhino[obj_, "Rhino.Geometry.Point3d[]"] := (* slow version *)
	Block[{enum = obj@GetEnumerator[]},
		Reap[While[enum@MoveNext[], Sow[FromRhino[enum@Current]]]][[2, 1]]
	]


(* Rhino.Geometry.Mesh *)


ToRhino[mesh_MeshRegion] :=
	Wolfram`Rhino`WolframScriptingPlugIn`ToRhinoMesh[
		MeshCoordinates[mesh],
		(First [#]-1)&/@ MeshCells[mesh,2]
	]


FromRhino[obj_, "Rhino.Geometry.Mesh"] :=
	MeshRegion[
		Wolfram`Rhino`WolframScriptingPlugIn`RhinoMeshVertices[obj],
		Polygon[Wolfram`Rhino`WolframScriptingPlugIn`RhinoMeshFaces[obj]]
	]


(* Rhino.DocObjects.CurveObject *)


FromRhino[obj_, "Rhino.DocObjects.CurveObject"] :=
	FromRhino[obj@Geometry]


(* Rhino.Geometry.NurbsCurve *)


FromRhino[obj_, "Rhino.Geometry.NurbsCurve"] :=
	Block[{degree, nControlPoints, controlPoints, nKnots, knots},
		degree = obj@Degree;
		nControlPoints = obj@Points@Count;
		controlPoints = {#@X,#@Y,#@Z}& /@ Table[obj@Points@Item[i]@Location,{i, 0, nControlPoints-1}];
		nKnots=obj@Knots@Count;
		knots = Table[obj@Knots@Item[i], {i, 0, nKnots-1}];
		BSplineCurve[
			controlPoints,
			SplineDegree->degree,
			SplineKnots->Join[{First[knots]}, knots, {Last[knots]}]
		]
	]


(* Rhino.Geometry.LineCurve *)


FromRhino[obj_, "Rhino.Geometry.LineCurve"] :=
	Line[{FromRhino@obj@PointAtStart, FromRhino@obj@PointAtEnd}]


(* Rhino.Geometry.PolylineCurve *)


FromRhino[obj_, "Rhino.Geometry.PolylineCurve"] :=
	Line[Table[FromRhino@obj@Point[i],{i,0,obj@PointCount-1}]]


(* Rhino.Geometry.PolyCurve *)


FromRhino[obj_, "Rhino.Geometry.PolyCurve"] :=
	GraphicsGroup[FromRhino/@ Table[obj@SegmentCurve[i], {i, 0, obj@SegmentCount-1}]]


(* *)


(* 
 * Queries
 *)


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


(* 
 * Rhino Geometric Operations 
 *)


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


(* 
 * Other Rhino Operations 
 *)


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


End[]


EndPackage[]
