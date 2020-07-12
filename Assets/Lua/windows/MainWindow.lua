local MainWindow = class("MainWindow", import(".BaseWindow"))
function MainWindow:ctor(name, params)
	MainWindow.super.ctor(self, name, params)
end

function MainWindow:initWindow()
	MainWindow.super.initWindow(self)
	self:getUIComponent()
end

function MainWindow:getUIComponent()
	local window_ = self.window_.transform
	self.btnConfirm = window_:NodeByName("@confirm").gameObject
	self.imgConfirm = window_:ComponentByName("@confirm", typeof(Image))
	local gameObject = window_:NodeByName("GameObject").gameObject
	self.imgDisplay = gameObject:ComponentByName("@display", typeof(Image))
	local groupBtn = window_:NodeByName("group_btn").gameObject
	self.btnGroupBtnConfirm = groupBtn:NodeByName("@confirm").gameObject
	self.imgConfirmDisplay = self.btnGroupBtnConfirm:ComponentByName("@display", typeof(Image))
	self.btnClose = groupBtn:NodeByName("@close").gameObject
	self.imgCloseDisplay = self.btnClose:ComponentByName("@display", typeof(Image))
end
