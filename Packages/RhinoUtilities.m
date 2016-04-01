(* ::Package:: *)

(* ::Text:: *)
(*Agenda*)
(**)
(*- complete nurbs curve conversions*)
(*- nurbs surfaces*)


BeginPackage["RhinoUtilities`", {"NETLink`"}]


LoadNETType["Rhino.RhinoDoc"];
LoadNETType["Wolfram.Rhino.WolframScriptingPlugIn"];


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


RhinoMeshUnion::usage = "RhinoMeshUnion[mesh1, mesh2] gives the mesh that is the union of mesh1 and mesh2.";


RhinoMeshIntersection::usage = "RhinoMeshIntersection[mesh1, mesh2] gives the mesh that is the intersection of mesh1 and mesh2.";


RhinoMeshDifference::usage = "RhinoMeshDifference[mesh1, mesh2] gives the mesh that is the difference of mesh1 and mesh2.";


RhinoMeshSplit::usage = "(doesn't appear to be working yet)";


(* 
 * Other Rhino Operations 
 *)


RhinoShow::usage = "RhinoShow[rhinoObject] adds 'rhinoObject' to the active document and releases it.";


RhinoUnshow::usage = "RhinoUnshow[guid] removed the object referenced by 'guid' from the active document and releases it.";


RhinoReshow::usage = "RhinoReshow[guid, rhinoObject] replaces the object referenced by 'guid' in the the active document with 'rhinoObject'.";


(* 
 *  
 *)


Begin["`Private`"]


(* 
 * Conversions
 *)


FromRhino[obj_Symbol] :=
	FromRhino[obj, obj@GetType[]@ToString[]]


FromRhino[objs_List] :=
	FromRhino /@ objs


FromRhino[objs_List, {type_}] :=
	FromRhino[#, type]& /@ objs


FromRhino[_, type_] :=
	$Failed[type]


ToRhino[expr_] :=
	ToRhino[expr, expr /. 
		(* default conversions *)
		{
			{_?NumericQ, _?NumericQ, _?NumericQ} -> "Rhino.Geometry.Point3d",
			{{_?NumericQ, _?NumericQ, _?NumericQ}..} -> {"Rhino.Geometry.Point3d"},
			_MeshRegion -> "Rhino.Geometry.Mesh",
			_BoundaryMeshRegion -> "Rhino.Geometry.Mesh",
			GraphicsComplex[{{_?NumericQ,_?NumericQ,_?NumericQ}...}, {_Polygon...}] -> "Rhino.Geometry.Mesh",
			_TransformationFunction -> "Rhino.Geometry.Transform"
		}
	]


ToRhino[expr:{___}, {type_}] :=
	ToRhino[#, type]& /@ expr


(* Rhino.Geometry.Transform *)


ToRhino[tf_TransformationFunction, "Rhino.Geometry.Transform"] :=
	Block[{m, t},
		m = TransformationMatrix[tf];
		t = NETNew["Rhino.Geometry.Transform", 1];
	
		t@M00 = m[[1,1]];
		t@M01 = m[[1,2]];
		t@M02 = m[[1,3]];
		t@M03 = m[[1,4]];
		t@M10 = m[[2,1]];
		t@M11 = m[[2,2]];
		t@M12 = m[[2,3]];
		t@M13 = m[[2,4]];
		t@M20 = m[[3,1]];
		t@M21 = m[[3,2]];
		t@M22 = m[[3,3]];
		t@M23 = m[[3,4]];
		t@M30 = m[[4,1]];
		t@M31 = m[[4,2]];
		t@M32 = m[[4,3]];
		t@M33 = m[[4,4]];

		t
	]


FromRhino[t_, "Rhino.Geometry.Transform"] :=
	{
		{t[M00], t[M01], t[M02], t[M03]},
		{t[M10], t[M11], t[M12], t[M13]},
		{t[M20], t[M21], t[M22], t[M23]},
		{t[M30], t[M31], t[M32], t[M33]}
	}


(* Rhino.Geometry.Vector3d *)


ToRhino[{x_, y_, z_}, "Rhino.Geometry.Vector3d"] :=
	NETNew["Rhino.Geometry.Vector3d", N[x], N[y], N[z]]


FromRhino[obj_, "Rhino.Geometry.Vector3d"] :=
	{obj@X, obj@Y, obj@Z}


(* Rhino.Geometry.Point3d *)


ToRhino[{x_, y_, z_}, "Rhino.Geometry.Point3d"] :=
	NETNew["Rhino.Geometry.Point3d", N[x], N[y], N[z]]


FromRhino[obj_, "Rhino.Geometry.Point3d"] :=
	{obj@X, obj@Y, obj@Z}


ToRhino[expr_, {"Rhino.Geometry.Point3d"}] :=
	WolframScriptingPlugIn`ToRhinoPoint3dArray[expr]


ToRhino[expr_, "Rhino.Geometry.Point3d[]"] :=
	ReturnAsNETObject@WolframScriptingPlugIn`ToRhinoPoint3dArray[expr]


(* ::Text:: *)
(*This cannot be wrapped with NETBlock to reclaim the Enumerator, or it will destroy the objects returned by the enumerator.*)


FromRhino[obj_, "Rhino.Geometry.Point3d[]"] := (* slow version *)
	Block[{enum = obj@GetEnumerator[]},
		Reap[While[enum@MoveNext[], Sow[FromRhino[enum@Current]]]][[2, 1]]
	]


(* Rhino.Geometry.Mesh *)


ToRhino[mesh_, "Rhino.Geometry.Mesh"] :=
	Wolfram`Rhino`WolframScriptingPlugIn`ToRhinoMesh[
		MeshCoordinates[mesh],
		(First[#] - 1)& /@ MeshCells[mesh, 2]
	]


ToRhino[GraphicsComplex[pts:{{_?NumericQ,_?NumericQ,_?NumericQ}...}, polygons:{_Polygon...}], "Rhino.Geometry.Mesh"] :=
	Wolfram`Rhino`WolframScriptingPlugIn`ToRhinoMesh[
		pts,
		(First[#] - 1)& /@ polygons
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


(* ::Text:: *)
(*This cannot be wrapped with NETBlock to reclaim the Enumerator, or it will destroy the objects returned by the enumerator.*)


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


(* ::Text:: *)
(*CNC: This doesn't appear to work.*)


RhinoMeshSplit[meshes1_,meshes2_]:=
	Rhino`Geometry`Mesh`CreateBooleanSplit[
		MakeNETObject[meshes1,"Rhino.Geometry.Mesh[]"],
		MakeNETObject[meshes2,"Rhino.Geometry.Mesh[]"]
	]


(* 
 * Other Rhino Operations 
 *)


RhinoAdd[obj_Symbol] :=
	{RhinoDoc`ActiveDoc@Objects@Add[obj]}


(* ::Text:: *)
(*These should just be listable, but there seemed to be an efficiency penalty; revisit.*)


RhinoAdd[objs_List] :=
	Flatten[RhinoDoc`ActiveDoc@Objects@Add[#]& /@ objs]


NETRelease[obj_Symbol] :=
	ReleaseNETObject[obj]


NETRelease[objs_List] :=
	ReleaseNETObject /@ objs


RhinoShow[obj_] :=
	Block[{guids},
		guids = RhinoAdd[obj];
		NETRelease[obj];
		RhinoDoc`ActiveDoc@Views@Redraw[];
		guids
	]


RhinoDelete[guid_Symbol] :=
	RhinoDoc`ActiveDoc@Objects@Delete[guid, True]


RhinoDelete[guids_List] :=
	RhinoDoc`ActiveDoc@Objects@Delete[#, True]& /@ guids


RhinoUnshow[guid_] :=
	Block[{},
		RhinoDelete[guid];
		RhinoDoc`ActiveDoc@Views@Redraw[];
	]


RhinoReshow[guid_, obj_] :=
	Block[{guids},
		RhinoDelete[guid];
		guids = RhinoAdd[obj];
		NETRelease[obj];
		RhinoDoc`ActiveDoc@Views@Redraw[];
		guids
	]


End[]


EndPackage[]
