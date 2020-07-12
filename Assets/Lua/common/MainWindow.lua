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
