//TODO FIX Issue for CHORDS -> The index is not being set correctly
Vex.UI.NoteMenu = function (handler, canvas, ctx) {
	this.handler = handler;
	this.canvas = canvas;
	this.ctx = ctx;
	this.note=null;
	this.keyIndex = null;
	this.panelProps = null;
	this.buttons = [];
	this.accidentals = ['doubleFlat', 'flat', 'natural', 'sharp', 'doubleSharp'];
	this.canRender = false;
	this.currentButton = null;
	this.buttonRenderer = new Vex.UI.NoteMenuButtonRenderer();
};

Vex.UI.NoteMenu.prototype.init = function() {
  // nothing to do atm
}