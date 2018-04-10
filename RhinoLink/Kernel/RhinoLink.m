(* ::Package:: *)

BeginPackage["RhinoLink`", {"NETLink`"}];


GHDeploy::usage = "";


GHResult::usage = "";


$RhinoHome::usage = "$RhinoHome is the path to the installation directory of Rhino. The default is \"c:\\Program Files\\Rhino 6\". You will need to set it to another value if you have a different location.";


$GrasshopperPath::usage = "$GrasshopperPath is the path to the directory containing Grasshopper DLLs (GH_IO.dll, Grasshopper.dll). It will have a default value suitable for Rhino 6, but can be changed if necessary.";


(* 
 * Data Type Conversions
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


Begin["`Private`"];


(*LoadNETType["Rhino.RhinoDoc"];
LoadNETType["Wolfram.Rhino.WolframScriptingPlugIn"];*)


If[!StringQ[$RhinoHome],
    $RhinoHome = "C:\\Program Files\\Rhino 6"
];

(* The path to the directory containing Grasshopper DLLs (GH_IO.dll, Grasshopper.dll) *)
If[!StringQ[$GrasshopperHome],
    $GrasshopperHome = FileNameJoin[{$RhinoHome, "Plug-ins", "Grasshopper"}]
];

(* This is the directory into which GHDeploy-ed components will be placed. Grasshopper automatically loads .gha files from here. *)
$deployDir = FileNameJoin[{ParentDirectory[$UserBaseDirectory], "Grasshopper", "Libraries"}];


$thisPacletDir = ParentDirectory[DirectoryName[$InputFileName]];


(**********************************  GHDeploy  *********************************)

GHDeploy::param:= "The parameter type `1` is not currently supported."

Options[GHDeploy] = {SaveDefinitions -> True, Initialization -> None, "Description" -> "User-generated Wolfram Language computation",  "Icon" -> Automatic}

(* These two defs allow users who are calling funcs with just one input or one output to leave off the outer wrapping braces. *)
GHDeploy[name_, func_, inputSpec_, outputSpec:{_String, _String, _String, _String, _}, opts:OptionsPattern[]] :=
    GHDeploy[name, func, inputSpec, {outputSpec}, opts]

GHDeploy[name_, func_, inputSpec:{_String, _String, _String, _String, _}, outputSpec_, opts:OptionsPattern[]] :=
    GHDeploy[name, func, {inputSpec}, outputSpec, opts]

(* Maybe the param spec should be "Text" -> {name, nick, desc, etc...}. Or maybe options for all values? *)
GHDeploy[name_String, func_, inputSpec:{{_String, _String, _String, _String, __}...},
                       outputSpec:{{_String, _String, _String, _String, _}..}, OptionsPattern[]] :=
    Module[{codeString, asmPath, saveDefs, initialization, icon, desc},
        {saveDefs, initialization, icon, desc} = OptionValue[{SaveDefinitions, Initialization, "Icon", "Description"}];
        If[icon === Automatic, icon = name];
        (* Fail right away if func is not of the correct form. Must be symbol or pure function. *)
        If[Head[func] =!= Symbol && Head[func] =!= Function,
            Message[GHDeploy::badfunc];
            Return[$Failed]
        ];
        (* TODO: Verify that all user-specified types are supported. *)
        codeString =
            TemplateApply[
                FileTemplate[FileNameJoin[{$thisPacletDir, "Files", "Component.cs"}]],
                <|"Name" -> name, "Nickname" -> name,  "Category" -> "Wolfram",
                  "Subcategory" -> "", "Description" -> desc,
                  "GUID" -> CreateUUID[],
                  "RegisterInputParams" -> StringJoin[addParam /@ inputSpec], 
                  "RegisterOutputParams" -> StringJoin[addParam /@ outputSpec],
                  "GetData" -> StringJoin[MapIndexed[getData, inputSpec]],
                  "InputArgCount" -> ToString[Length[inputSpec]],
                  "UseInit" -> If[initialization =!= None, "true", "false"],
                  "Initialization" -> If[initialization =!= None, ToString[initialization, InputForm], "\"\""],
                  "UseDefs" -> If[TrueQ[saveDefs], "true", "false"],
                  (* NOTE: ugly call into private CloudObject functionality. Fix. *)
                  "Definitions" -> If[TrueQ[saveDefs], ToString[exprToStringWithSaveDefinitions[func], InputForm], ""],
                  "HeadIsSymbol" -> If[Head[func] === Symbol, "true", "false"],
                  "Func" -> ToString[func, InputForm],
                  "SendInputParams" -> StringJoin[MapIndexed[sendInputParam, inputSpec]],
                  "ReadAndStoreResults" -> StringJoin[MapIndexed[readAndStoreResult, outputSpec]]
                |>
            ];
Global`code = codeString;
        asmPath = generateComponentAssembly[name, codeString, icon];
        If[StringQ[asmPath],
            deployComponentAssembly[asmPath];
            Null,
        (* else *)
            $Failed
        ]
    ]
    
(* TODO: accessType for all of these (list/item). 
         Issue message on bad default value.
         Coalesce these into a single function; they are almost identical.       
*)

addParam[{"Text", name_String, nickname_String, description_String, accessType_, default:_:None}] :=
    Module[{},
        TemplateApply[StringTemplate["pManager.AddTextParameter(\"`Name`\", \"`Nickname`\", \"`Description`\", `Access` `Default`);\n"],
                <|"Name" -> name, "Nickname" -> nickname, "Description" -> description,
                  "Access" -> accessTypeName[accessType],
                  "Default" -> If[StringQ[default], ", " <> ToString[default, InputForm], ""]
                |>
        ]
    ]
    
addParam[{"Integer", name_String, nickname_String, description_String, accessType_, default:_:None}] :=
    Module[{},
        TemplateApply[StringTemplate["pManager.AddIntegerParameter(\"`Name`\", \"`Nickname`\", \"`Description`\", `Access` `Default`);\n"],
                <|"Name" -> name, "Nickname" -> nickname, "Description" -> description, 
                  "Access" -> accessTypeName[accessType],
                  "Default" -> If[IntegerQ[default], ", " <> ToString[default, InputForm], ""]
                |>
        ]
    ]
    
addParam[{"Number", name_String, nickname_String, description_String, accessType_, default:_:None}] :=
    Module[{},
        TemplateApply[StringTemplate["pManager.AddNumberParameter(\"`Name`\", \"`Nickname`\", \"`Description`\", `Access` `Default`);\n"],
                <|"Name" -> name, "Nickname" -> nickname, "Description" -> description,
                  "Access" -> accessTypeName[accessType],
                  "Default" -> If[NumberQ[default], ", " <> ToString[N[default], InputForm], ""]
                |>
        ]
    ]
    
addParam[{"Boolean", name_String, nickname_String, description_String, accessType_, default:_:None}] :=
    Module[{},
        TemplateApply[StringTemplate["pManager.AddBooleanParameter(\"`Name`\", \"`Nickname`\", \"`Description`\", `Access` `Default`);\n"],
                <|"Name" -> name, "Nickname" -> nickname, "Description" -> description,
                  "Access" -> accessTypeName[accessType],
                  "Default" -> If[NumberQ[default], ", " <> ToString[N[default], InputForm], ""]
                |>
        ]
    ]
    
addParam[{"Any", name_String, nickname_String, description_String, accessType_, default:_:None}] :=
    Module[{},
        TemplateApply[StringTemplate["pManager.AddGenericParameter(\"`Name`\", \"`Nickname`\", \"`Description`\", `Access` `Default`);\n"],
                <|"Name" -> name, "Nickname" -> nickname, "Description" -> description,
                  "Access" -> accessTypeName[accessType],
                  "Default" -> "" (* No support for a default value in AddGenericParameter. *)
                |>
        ]
    ]
        
addParam[{"Expr", name_String, nickname_String, description_String, accessType_, default:_:None}] :=
    Module[{},
        TemplateApply[
            StringTemplate[
                "pManager.AddParameter(new ExprParam(), \"`Name`\", \"`Nickname`\", \"`Description`\", `Access` `Default`);\n"
            ],
            <|"Name" -> name, "Nickname" -> nickname, "Description" -> description,
              "Access" -> accessTypeName[accessType],
              "Default" -> "" (* No support for a default value in AddParameter. *)
            |>
        ]
    ]
    
addParam[{unsupported_String, nickname_String, description_String, accessType_, default:_:None}] :=
    (Message[GHDeploy::param, unsupported]; $Failed)


accessTypeName[sym_Symbol] := accessTypeName[SymbolName[sym]]

accessTypeName[type_String] :=
    Switch[type,
        "Tree", "GH_ParamAccess.tree",
        "List", "GH_ParamAccess.list",
        _, "GH_ParamAccess.item"
    ]
    

argDeclaration["Text", argName_, "GH_ParamAccess.item"] := "string " <> argName <> " = null;\n"
argDeclaration["Integer", argName_, "GH_ParamAccess.item"] := "int " <> argName <> " = 0;\n"
argDeclaration["Number", argName_, "GH_ParamAccess.item"] := "double " <> argName <> " = 0.0;\n"
argDeclaration["Boolean", argName_, "GH_ParamAccess.item"] := "bool " <> argName <> " = false;\n"
argDeclaration["Any", argName_, "GH_ParamAccess.item"] := "object " <> argName <> " = null;\n"
argDeclaration["Expr", argName_, "GH_ParamAccess.item"] := "ExprType " <> argName <> " = null;\n"
argDeclaration[_, argName_, "GH_ParamAccess.list"] := "List<IGH_Goo> " <> argName <> " = new List<IGH_Goo>();\n"
argDeclaration[_, argName_, "GH_ParamAccess.tree"] := "GH_Structure<IGH_Goo> " <> argName <> " = new GH_Structure<IGH_Goo>();\n"

getData[{type_, name_, nick_, desc_, accessType_, default:_:None}, {index_Integer}] := 
    Module[{argName = "arg" <> ToString[index]},
        argDeclaration[type, argName, accessTypeName[accessType]] <>
        Switch[accessTypeName[accessType],
            "GH_ParamAccess.list",
                TemplateApply[
                    StringTemplate[
                        "if (!DA.GetDataList(`indexMinusOne`, `argName`)) return;\n"
                    ],
                    <|"argName" -> argName, "indexMinusOne" -> ToString[index-1]|>
                ],
            "GH_ParamAccess.tree",
                TemplateApply[
                    StringTemplate[
                        "if (!DA.GetDataTree(`indexMinusOne`, out `argName`)) return;\n"
                    ],
                    <|"argName" -> argName, "indexMinusOne" -> ToString[index-1]|>
                ],
            _, (* "GH_ParamAccess.item" *)
                TemplateApply[
                    StringTemplate[
                        "if (!DA.GetData(`indexMinusOne`, ref `argName`)) return;
                         if (`argName` == null) return;\n"
                    ],
                    <|"argName" -> argName, "indexMinusOne" -> ToString[index-1]|>
                ]
        ]
    ]


sendInputParam[inputSpec:{_String, _String, _String, _String, accessType_, ___}, {index_Integer}] :=
    Module[{},
        "Utils.SendInputParam(arg" <> ToString[index] <> ", link, " <> accessTypeName[accessType] <> ", false);\n"
    ]
    

readAndStoreResult[{type_String, _, _, _, accessType_}, {index_Integer}] :=
    "if (!Utils.ReadAndStoreResult(\"" <> type <> "\", " <> ToString[index-1] <> ", link, DA, " <> accessTypeName[accessType] <> ", this)) return;\n"


(*
    neutralContextBlock[expr] evalutes expr without any contexts on the context path,
    to ensure symbols are serialized including their context (except System` symbols).
*)
Attributes[neutralContextBlock] = {HoldFirst};
neutralContextBlock[expr_] := Block[{$ContextPath = {"System`"}, $Context = "System`"}, expr]

(* Borrowed with some simplifications from internal code in the CloudObject package. *)
exprToStringWithSaveDefinitions[expr_] :=
    Module[{defs, defsString, exprLine},
        defs = Language`ExtendedFullDefinition[expr];
        defsString = If[defs =!= Language`DefinitionList[],
            neutralContextBlock[With[{d = defs},
                (* Language`ExtendedFullDefinition[] can be used as the LHS of an assignment to restore
                 * all definitions. *)
                ToString[Unevaluated[Language`ExtendedFullDefinition[] = d], InputForm,
                    CharacterEncoding -> "PrintableASCII"]
            ]] <> ";\n\n",
        (* else *)
            ""
        ];
        exprLine = neutralContextBlock[ToString[Unevaluated[expr], InputForm, CharacterEncoding -> "PrintableASCII"]];
        StringTrim[defsString <> exprLine] <> "\n"
    ]


$filledRightTriangle = ToString[\[FilledRightTriangle]];

generateComponentAssembly[componentName_String, code_String, icon_] :=
NETBlock[
    Module[{provider, providerOptions, params, compilerResults, err, ct, scode, resFile},
     
        resFile = generateResourcesFile[icon];

        providerOptions = NETNew["System.Collections.Generic.Dictionary`2[string, string]"];
        providerOptions@Add["CompilerVersion", "v4.0"];
        provider = NETNew["Microsoft.CSharp.CSharpCodeProvider", providerOptions];
		  
        params = NETNew["System.CodeDom.Compiler.CompilerParameters"];
        params @ ReferencedAssemblies @ Add["System.dll"];
        params @ ReferencedAssemblies @ Add["System.Core.dll"];
        params @ ReferencedAssemblies @ Add["System.Drawing.dll"];
        params @ ReferencedAssemblies @ Add["System.Windows.Forms.dll"];
        params @ ReferencedAssemblies @ Add[FileNameJoin[{$GrasshopperHome, "GH_IO.dll"}]];
        params @ ReferencedAssemblies @ Add[FileNameJoin[{$GrasshopperHome, "Grasshopper.dll"}]];
        params @ ReferencedAssemblies @ Add[FileNameJoin[{$RhinoHome, "System", "rhinocommon.dll"}]];
        params @ ReferencedAssemblies @ Add[FileNameJoin[{$thisPacletDir, "Libraries", "Rhino", "Wolfram.NETLink.dll"}]];
        params @ ReferencedAssemblies @ Add[FileNameJoin[{$thisPacletDir, "Libraries", "Grasshopper", "WolframGrasshopperSupport.dll"}]];
		  
        params@EmbeddedResources@Add[resFile];
        params@GenerateInMemory = False;
        params@OutputAssembly = FileNameJoin[{$TemporaryDirectory, componentName <> ".gha"}];
		
		compilerResults = provider@CompileAssemblyFromSource[params, {code}];
        provider@Dispose[];
        DeleteFile[resFile];
		  
        err = compilerResults@Errors;
        ct = err@Count;
        If[ct > 0,
            scode = StringSplit[code, "\n"];
            Print["number of errors = ", ct];
            Print[ 
                "Error: ",
                err[#]@ErrorText,
                "\nLine: ",
                err[#]@Line,
                "\n",
                StringJoin @ 
                    Insert[Characters[scode[[err[#]@Line]]], 
                    $filledRightTriangle, err[#]@Column]
            ]& /@ Range[0, ct - 1];
            Return[$Failed]
        ];
        
        compilerResults@PathToAssembly
    ]
]


generateResourcesFile[icon_] :=
    NETBlock[
        Module[{iconGraphic, iconData, resWriter, resFile},
            resFile = FileNameJoin[{$TemporaryDirectory, "gh.resources"}];
            (* prepare icon file. *)
            If[Head[icon] === Graphics,
                (* If it's already a Graphics, resize to 24x24. *)
                iconGraphic = ImageResize[icon, {24, 24}],
            (* else *)
                iconGraphic = Graphics[{Text[Style[icon, {Bold, Larger}]]}, ImageSize->{24,24}]
            ];
            iconData = ToCharacterCode[ExportString[iconGraphic, "PNG"]];
            resWriter = NETNew["System.Resources.ResourceWriter", resFile];
            resWriter@AddResource["Icon", MakeNETObject[iconData, "System.Byte[]"]];
            resWriter@Close[];
            resFile
        ]
    ]
 
 
deployComponentAssembly[assemblyPath_String] :=
    Module[{asmName},
        asmName = FileNameTake[assemblyPath];
        If[FileExistsQ[FileNameJoin[{$deployDir, asmName}]],
            DeleteFile[FileNameJoin[{$deployDir, asmName}]]
        ];
        CopyFile[assemblyPath, FileNameJoin[{$deployDir, asmName}]];
        (* Copy needed libraries if they are not already present. *)
        If[!FileExistsQ[FileNameJoin[{$deployDir, "WolframGrasshopperSupport.dll"}]],
            CopyFile[FileNameJoin[{$thisPacletDir, "Files", "WolframGrasshopperSupport.dll"}], FileNameJoin[{$deployDir, "WolframGrasshopperSupport.dll"}]]
        ];
        If[!FileExistsQ[FileNameJoin[{$deployDir, "WolframGrasshopperComponents.gha"}]],
            CopyFile[FileNameJoin[{$thisPacletDir, "Files", "WolframGrasshopperComponents.gha"}], FileNameJoin[{$deployDir, "WolframGrasshopperComponents.gha"}]]
        ];
        If[!FileExistsQ[FileNameJoin[{$deployDir, "Wolfram.NETLink.dll"}]],
            CopyFile[FileNameJoin[{$thisPacletDir, "Files", "Wolfram.NETLink.dll"}], FileNameJoin[{$deployDir, "Wolfram.NETLink.dll"}]]
        ];
        If[!FileExistsQ[FileNameJoin[{$deployDir, "ml32i4.dll"}]],
            CopyFile[FileNameJoin[{$thisPacletDir, "Files", "ml32i4.dll"}], FileNameJoin[{$deployDir, "ml32i4.dll"}]]
        ];
        If[!FileExistsQ[FileNameJoin[{$deployDir, "ml64i4.dll"}]],
            CopyFile[FileNameJoin[{$thisPacletDir, "Files", "ml64i4.dll"}], FileNameJoin[{$deployDir, "ml64i4.dll"}]]
        ]
    ]
    
    
    
(* Called from C# Wolfram script. Sets up the link from a launched kernel that waits for an FE to attach. *)
setupRhinoAttachLink[] :=
    Module[{link},
        debug["Entering setuplinksfromunity"];
        link = LinkCreate["RhinoAttach"];
        If[Head[link] === LinkObject,
            MathLink`AddSharingLink[link, MathLink`SendInputNamePacket -> True]
        ];
        debug["attach: " <> ToString[link]];
        
        debug["at start, previous link was : " <> ToString[$previousLink]];
        If[Head[$previousLink] === LinkObject,
            (* Typically, was already closed in unityDetach[] from last session. *)
            MathLink`RemoveSharingLink[$previousLink];
            LinkClose[$previousLink];
            debug["just closed " <> ToString[$previousLink]]
        ];
        
        (*prepareLinkForUnityToConnect[];*)
        
        $previousLink = $ParentLink;
        debug["link waiting for next time: " <> ToString[link]];
        debug["current link, which wil lbe closed next time " <> ToString[$previousLink]];
        
        (* It is important to make preemptive all the links that could get U-to-M traffic. Otherwise it is easy
           to get deadlock when calling from Wolfram into Unity at the same time a user script is calling
           into Wolfram.
        *)
        MathLink`AddSharingLink[$ParentLink, MathLink`AllowPreemptive -> True];
    ]
    
    
    (* Called from C# Wolfram script *)
setupReaderLink[] :=
    (
        $readerLink = LinkCreate[];
        LinkWrite[$ParentLink, First[$readerLink]];
        LinkConnect[$readerLink];
        NETLink`InstallNET`Private`$nlink = $readerLink;
        NETLink`InstallNET`Private`$uilink = $ParentLink;
    )



(* 
 * Data Type Conversions
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
	
		t@M00 = m[[1,1]]; t@M01 = m[[1,2]]; t@M02 = m[[1,3]]; t@M03 = m[[1,4]];
		t@M10 = m[[2,1]]; t@M11 = m[[2,2]]; t@M12 = m[[2,3]]; t@M13 = m[[2,4]];
		t@M20 = m[[3,1]]; t@M21 = m[[3,2]]; t@M22 = m[[3,3]]; t@M23 = m[[3,4]];
		t@M30 = m[[4,1]]; t@M31 = m[[4,2]]; t@M32 = m[[4,3]]; t@M33 = m[[4,4]];

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


(* 
 * Queries
 *)


(* ::Text:: *)
(*This cannot be wrapped with NETBlock to reclaim the Enumerator, or it will destroy the objects returned by the enumerator.*)


RhinoDocObjects[doc_:RhinoDoc`ActiveDoc]:=
	With[{it = doc@Objects@GetEnumerator[]},
		Flatten[Reap[While[it@MoveNext[], Sow[it@Current]]][[2]]]
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

