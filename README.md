# ComponentAutoBindLua
### 用法
1. 在cs代码中配置_componentMap用于导出需要的组件,配置_nodeMap用于导出需要的节点
2. 在prefab需要导出的节点名字前加上@，名字带@的节点会被扫描所带的组件是否有所需要被导出的组件，如果有的话会自动导出lua代码
3. 右击ui节点会出现**Lua Code Generate**
**Generate Window Lua Component Code**用于生成继承BaseWindow的窗口prefab的lua代码
**Generate Other Lua Component Code**用于生成继承BaseComponent的物体prefab的lua代码
### 介绍
- 生成规则
    - 名字带@的会被导出为类内成员
    - 名字不带@但是子节点需要被导出则会被导出为local变量
    -- 导出的名字都为驼峰命名
- 生成部分包括定义
   - Window
    ```lua
    local Xxx = class("Xxx", import(".BaseWindow"))

    function Xxx:ctor(name, params)
        Xxx.super.ctor(self, name, params)
    end

    function XxxWindow:initWindow()
        Xxx.super.initWindow(self)

        self:getUIComponent()
    end

    function Xxx:getUIComponent()
        -- 自动绑定代码
    end
    ```
    - Other
    ```lua
    local Xxx = class("Xxx", import(".BaseComponent"))

    function Xxx:ctor(parentGo)
        Xxx.super.ctor(self, parentGo)
    end

    function Xxx:initGO()
        Xxx.super.initGO(self)
        self:getUIComponent()
    end

    function Xxx:getUIComponent()
        -- 自动绑定代码
    end
    ```

### 示例
1. UI节点示意图
  - main_window
    - gameobject
    - @confirm(button, image)
    - gameobject
      - @display(image)
    - group_btn
      - @confirm(button)
        - @display(image)
      - @close(button)
        - @display(image)
2. 导出的代码
  ```lua
    local MainWindow = class("MainWindow", import(".BaseComponent"))
    function MainWindow:ctor(parentGo)
    	MainWindow.super.ctor(self, parentGo)
    end

    function MainWindow:initGO()
    	MainWindow.super.initGO(self)
    	self:getUIComponent()
    end

    function MainWindow:getUIComponent()
    	local go = self.go
    	self.btnConfirm = go:NodeByName("@confirm").gameObject
    	self.imgConfirm = go:ComponentByName("@confirm", typeof(Image))
    	local gameObject = go:NodeByName("GameObject").gameObject
    	self.imgDisplay = gameObject:ComponentByName("@display", typeof(Image))
    	local groupBtn = go:NodeByName("group_btn").gameObject
    	self.btnGroupBtnConfirm = groupBtn:NodeByName("@confirm").gameObject
    	self.imgConfirmDisplay = self.btnGroupBtnConfirm:ComponentByName("@display", typeof(Image))
    	self.btnClose = groupBtn:NodeByName("@close").gameObject
    	self.imgCloseDisplay = self.btnClose:ComponentByName("@display", typeof(Image))
    end
  ```