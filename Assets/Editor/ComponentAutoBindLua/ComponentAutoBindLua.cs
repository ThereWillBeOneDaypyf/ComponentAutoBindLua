using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace XINYOUDI
{
    struct TransformInfo
    {
        public string luaName;
        public Transform transform;
        public bool needExport;
    }
    class ComponentAutoBindLua
    {
        [MenuItem("GameObject/Lua Code Generate/Generate Window Lua Component Code", priority = 0)]
        static void GenerateWindowCode()
        {
            DoTask(true);
        }
        [MenuItem("GameObject/Lua Code Generate/Generate Other Lua Component Code", priority = 0)]
        static void GenerateOtherCode()
        {
            DoTask(false);
        }

        static void DoTask(bool isWin)
        {
            var list = Selection.gameObjects;
            if(list.Length < 1)
                return;
            GameObject go = Selection.gameObjects[0];
            var travelHelper = new GameObjectTravelHelper(go.transform, isWin);
            Dictionary<string, List<TransformInfo> > tree = travelHelper.GetTree(); 
            string rootName;
            if (isWin)
                rootName = "window_";
            else
                rootName = "go";
            LuaWriter luaWriter = new LuaWriter(tree, ComponentNameHelper.generateNameFromGameObjectName(go.transform), rootName, isWin);
            luaWriter.Write();
        }
    }

    class GameObjectTravelHelper
    {
        // luaname -> true
        private Dictionary<string, bool> _nameMap = new Dictionary<string, bool>();
        // luaname -> transformInfo
        private Dictionary<string, List<TransformInfo>> _treeMap = new Dictionary<string, List<TransformInfo>>();
        private bool _isWin = true;
        private bool _hasTravel = false;
        private Transform _root;
        public GameObjectTravelHelper(Transform t)
        {
            _root = t;
        }

        public GameObjectTravelHelper(Transform t, bool isWin)
        {
            _root = t;
            _isWin = isWin;
        }

        void TravelDown(Transform transform, string pLuaName, List<string> namesInPath)
        {
            foreach(Transform childTrans in transform) 
            {
                if(!checkNeedExport(childTrans))
                    continue;
                bool needExport = (childTrans.gameObject.name[0] == '@');
                var curName = ComponentNameHelper.generateNameFromGameObjectName(childTrans);
                curName = ComponentNameHelper.rebuildNameForGameObject(curName, _nameMap, namesInPath);
                _nameMap.Add(curName, true);
                TransformInfo info = new TransformInfo();
                info.needExport = needExport;
                info.luaName = curName;
                info.transform = childTrans;
                if(!_treeMap.ContainsKey(pLuaName))
                    _treeMap[pLuaName] = new List<TransformInfo>();
                _treeMap[pLuaName].Add(info);
                namesInPath.Add(childTrans.gameObject.name);
                TravelDown(childTrans,curName, namesInPath);
                namesInPath.RemoveAt(namesInPath.Count - 1);
            }
        }

        private bool checkNeedExport(Transform trans)
        {
            if(trans.gameObject.name[0] == '@')
                return true;
            foreach(Transform t in trans)
            {
                if(checkNeedExport(t))
                    return true;
            }
            return false;
        }

        public Dictionary<string, List<TransformInfo> > GetTree()
        {
            if(!_hasTravel) 
            {
                var names = new List<string>();
                string rootName;
                if(_isWin) 
                    rootName = "window_";
                else
                    rootName = "go";
                _nameMap.Add(rootName, true);
                names.Add(rootName);
                TravelDown(_root, rootName, names);
                _hasTravel = true;
            }
            return _treeMap;
        } 
    }

    class ComponentNameHelper 
    {
        public static string generateNameFromGameObjectName(Transform t)
        {
            var curName = t.gameObject.name;
            return ComponentNameHelper.generateNameFromGameObjectName(curName);
        }

        public static string generateNameFromGameObjectName(string goName)
        {
            string curName = goName;
            if(curName[0] == '@')
            {
                curName = curName.Substring(1);
            }
            var smallNames = curName.Split('_');
            string resName = "";
            foreach(var s in smallNames)
            {
                var changableS = s.ToCharArray();
                changableS[0] = Char.ToUpper(s[0]);
                resName += new String(changableS); 
            }
            return resName;

        }

        public static string changeFirstToSmall(string s)
        {
            if(!Char.IsLetter(s[0]))
                return s;
            var charArray = s.ToCharArray();
            charArray[0] = Char.ToLower(s[0]);
            return "" + new String(charArray);
        }

        public static string rebuildNameForGameObject(string name, Dictionary<string, bool> map, List<string> names)
        {
            string resName = name;
            int index = names.Count;
            for(int i = index - 1; i >= 0; i --)
            {
                if(map.ContainsKey(resName))
                    resName = ComponentNameHelper.generateNameFromGameObjectName(names[i]) + resName; 
                else
                    break;
            }
            return resName;
        }
    }

    /*
    local className = class(className, import(super))
    function className:ctor()
    end
    function className:initWindow()
    end
    function className:initGO()
    end
    function className:getUIComponent()
    end
    */
    class LuaWriter
    {
        private Dictionary<string, string> _componentMap = new Dictionary<string, string>{
            {typeof(UnityEngine.UIElements.Image).Name, "img"},
        };
        private Dictionary<string, string> _nodeMap = new Dictionary<string, string> {
            {typeof(UnityEngine.UIElements.Button).Name, "btn"}
        };
        private string DEFINATION_FORMAT_STRING = "local {0} = class(\"{0}\", import(\"{1}\"))";
        private string WIN_CONSTRUCTOR_FORMAT_STRING = "function {0}:ctor(name, params)\n\t{0}.super.ctor(self, name, params)\nend\n\nfunction {0}:initWindow()\n\t{0}.super.initWindow(self)\n\tself:getUIComponent()\nend";
        private string OTHER_CONSTRUCTOR_FORMAT_STRING = "function {0}:ctor(parentGo)\n\t{0}.super.ctor(self, parentGo)\nend\n\nfunction {0}:initGO()\n\t{0}.super.initGO(self)\n\tself:getUIComponent()\nend";
        private string LOCAL_NODE_LUA_STRING = "local {0} = {1}:NodeByName(\"{2}\").gameObject";
        private string LOCAL_COMPONENT_LUA_STRTING = "local {0} = {1}:ComponentByName(\"{2}\", typeof({3}))";
        private string SELF_NODE_LUA_STRING = "self.{0} = {1}:NodeByName(\"{2}\").gameObject";
        private string SELF_COMPONENT_LUA_STRING = "self.{0} = {1}:ComponentByName(\"{2}\", typeof({3}))";
        private string LUA_WINDOW_FILE_PATH_STRING = "Assets\\Lua\\windows\\{0}";
        private string LUA_COMPONENT_FILE_PATH_STRING = "Assets\\Lua\\common\\{0}";
        private string _fileNameWithoutExt;
        private string _rootName;
        private StringBuilder _definationStringBuilder = new StringBuilder();
        private StringBuilder _constructorStringBuilder = new StringBuilder();
        private StringBuilder _componentStringBuilder = new StringBuilder();
        private Dictionary<string, List<TransformInfo>> _treeMap = new Dictionary<string, List<TransformInfo>>();
        private bool _isWin;
        private  string _goName;
        public LuaWriter(Dictionary<string, List<TransformInfo> > treeMap, string goName, string rootName, bool isWin)
        {
            _treeMap = treeMap;
            _isWin = isWin;
            _rootName = rootName;
            _goName = goName;
        }

        public void Write()
        {
            WriteDefination(_goName);
            WriteConstructor(_goName);
            TravelComponent();
            string filePath;
            if (_isWin)
            {
                filePath = string.Format(LUA_WINDOW_FILE_PATH_STRING, _goName);
            }
            else
            {
                filePath = string.Format(LUA_COMPONENT_FILE_PATH_STRING, _goName);
            }
            WriteDownFile(filePath);
        }

        private string protectFile(string filePathWithoutExt)
        {
            if(!System.IO.File.Exists(filePathWithoutExt + ".lua"))
            {
                return filePathWithoutExt;
            }
            int idx = 1;
            for(;;idx ++)
            {
                if(!System.IO.File.Exists(filePathWithoutExt + idx.ToString() + ".lua"))
                {
                    return filePathWithoutExt + idx.ToString();
                }
            }
        }

        private void WriteDownFile(string filePathWithoutExt)
        {
            filePathWithoutExt = protectFile(filePathWithoutExt);
            FileStream fs = new FileStream(filePathWithoutExt + ".lua", FileMode.Create);
            StreamWriter sw = new StreamWriter(fs, Encoding.Default);

            sw.Write(_definationStringBuilder);
            sw.Write(_constructorStringBuilder);
            sw.Write(_componentStringBuilder);

            sw.Close();
            fs.Close();
        }

        private void WriteDefination(string className)
        {
            StringWriter writer = new StringWriter(_definationStringBuilder);
            string baseClass;
            if(_isWin)
                baseClass = ".BaseWindow";
            else
                baseClass = ".BaseComponent";
            writer.WriteLine(String.Format(DEFINATION_FORMAT_STRING, className, baseClass));
        }

        private void WriteConstructor(string className)
        {
            StringWriter write = new StringWriter(_constructorStringBuilder);
            string formatString;
            if(_isWin)
                formatString = WIN_CONSTRUCTOR_FORMAT_STRING;
            else
                formatString = OTHER_CONSTRUCTOR_FORMAT_STRING;
            write.WriteLine(string.Format(formatString, className));
            write.WriteLine("");
        }

        private void TravelComponent()
        {
            StringWriter writer = new StringWriter(_componentStringBuilder);
            writer.WriteLine(string.Format("function {0}:getUIComponent()", _goName));
            if(_isWin)
                writer.WriteLine("\tlocal window_ = self.window_.transform");
            else
                writer.WriteLine("\tlocal go = self.go");
            var list = _treeMap[_rootName];
            foreach (var item in list)
            {
                DoTravel(_rootName, item); 
            }
            writer.WriteLine("end");
        }

        private void DoTravel(string parentClassName, TransformInfo info, bool parentIsSelf = false)
        {
            if(!checkNeedExport(info))
                return;
            var result = WriteComponent(parentClassName, info, parentIsSelf);
            if (!_treeMap.ContainsKey(info.luaName))
                return;
            var list = _treeMap[info.luaName];
            var className = result.Item2;
            if(className == null)
                className = info.luaName;
            foreach (var item in list)
            {
                DoTravel(className, item, result.Item1);
            }
        }

        private bool checkNeedExport(TransformInfo info)
        {
            if(info.needExport)
                return true;
            if(!_treeMap.ContainsKey(info.luaName))
                return false;
            var list = _treeMap[info.luaName];
            foreach(var item in list)
            {
                if(checkNeedExport(item))
                {
                    return true;
                }
            }
            return false;
        }

        private Tuple<bool, string> WriteComponent(string parentClassName, TransformInfo info, bool parentIsSelf)
        {
            var list = info.transform.gameObject.GetComponents<Component>();
            string className = info.luaName;
            bool hasExportNode = false;
            bool isSelf = true;
            string nodeName = null;
            parentClassName = ComponentNameHelper.changeFirstToSmall(parentClassName);
            if(parentIsSelf)
                parentClassName = "self." + parentClassName;
            foreach(var comp in list)
            {
                if(_componentMap.ContainsKey(comp.GetType().Name))
                {
                    string prefix = _componentMap[comp.GetType().Name];
                    DoWriteComponent(parentClassName, prefix + className, info.transform.gameObject.name, comp.GetType().Name, info.needExport);
                }

                if(_nodeMap.ContainsKey(comp.GetType().Name) && !hasExportNode)
                {
                    string prefix = _nodeMap[comp.GetType().Name];
                    DoWriteComponent(parentClassName, prefix + className, info.transform.gameObject.name, info.needExport);
                    hasExportNode = true;
                    nodeName = prefix + className;
                }
            }
            if(!info.needExport && !hasExportNode)
            {
                isSelf = false;
                DoWriteComponent(parentClassName, ComponentNameHelper.changeFirstToSmall(className), info.transform.gameObject.name, info.needExport);
            }
            return new Tuple<bool, string>(isSelf, nodeName);
        }

        private void DoWriteComponent(string parentClassName, string className, string gameObjectName, string typeName, bool isSelf)
        {
            StringWriter writer = new StringWriter(_componentStringBuilder);
            string formatString;
            if(!isSelf)
                formatString = LOCAL_COMPONENT_LUA_STRTING;
            else
                formatString = SELF_COMPONENT_LUA_STRING;
            writer.WriteLine("\t" + string.Format(formatString, className, parentClassName, gameObjectName, typeName));
        }

        private void DoWriteComponent(string parentClassName, string className, string gameObjectName, bool isSelf)
        {
            StringWriter writer = new StringWriter(_componentStringBuilder);
            string formatString;
            if(!isSelf)
                formatString = LOCAL_NODE_LUA_STRING;
            else
                formatString = SELF_NODE_LUA_STRING;
            writer.WriteLine("\t" + string.Format(formatString, className, parentClassName, gameObjectName));
        }

    }
}