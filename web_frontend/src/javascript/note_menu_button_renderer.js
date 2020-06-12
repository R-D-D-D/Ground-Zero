Vex.UI.NoteMenuButtonRenderer = function (panel, context){
	this.ready = false;
	var renderer = this;
	//Load Resources
	this.menuButtonsImg = new Image();
	this.menuButtonsImg.src = 'https://cdn.jsdelivr.net/gh/R-D-D-D/Ground-Zero/web_frontend/src/resources/noteMenuButtons.gif';
	this.menuButtonsImg.onload = function(){renderer.ready=true};
};
