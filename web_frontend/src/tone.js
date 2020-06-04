const tone = {
  lowPass: new Tone.Filter({
    frequency: 11000,
  }),

  noise: new Tone.NoiseSynth({
    volume: -12,
    noise: {
      type: 'pink',
      playbackRate: 3,
    },
    envelope: {
      attack: 0.001,
      decay: 0.13,
      sustain: 0,
      release: 0.03,
    },
  }),

  poly: new Tone.PolySynth(6, Tone.Synth, {
    volume: -10,
    oscillator: {
      partials: [0, 2, 3, 4],
    },
    envelope: {
      attack: 0.001,
      decay: 0.17,
      sustain: 0,
      release: 0.05,
    },
  }),

  player: null,

  freqEnv: [],

  notesToEvents(notes) {
    var result = [];
    var time = 0;
    notes.forEach(element => {
      var temp = element.split("/")[2];
      var isRest = false;
      var isDot = false;
      if (temp.includes("d") || temp.include("r")) {
        if (temp.includes("r")) {
          temp.replace("r", "");
          isRest = true;
        }
        if (temp.includes("d")) {
          temp.replace("d", "");
          isDot = true;
        }
      }
      if (temp.includes("q")) {

      } else if (temp.includes("h")) {

      } else if (temp.includes("f")) {

      } else {

      }
    });
  },

  init(play, player) {
    this.player = player;
    this.poly.voices.forEach((v, i) => {
      const env = new Tone.FrequencyEnvelope({
        attack: 0.001,
        decay: 0.08,
        release: 0.08,
        baseFrequency: Tone.Frequency("A4"),
        octaves: Math.log2(13),
        releaseCurve: 'exponential',
        exponent: 3.5,
      });
      env.connect(v.oscillator.frequency);
      this.freqEnv[i] = env;
    });
    this.noise.connect(this.lowPass);
    this.lowPass.toMaster();
    this.poly.toMaster();
    this.player.toMaster();
  },

  createAndRecordSequence(bpm, notes, numBars, audio) {
    // simple check to avoid double play
    var started = false;
    if (started) return;

    Tone.Transport.bpm.value = bpm;
    const part = new Tone.Part((time) => {
      //the events will be given to the callback with the time they occur
      this.poly.voices.forEach((v, i) => {
        this.freqEnv[i].triggerAttackRelease('16n', time);
        v.envelope.triggerAttackRelease('16n', time);
      });
      this.noise.triggerAttackRelease('16n', time);
      }, [{ time : 0, note : 'A4', dur : '16n'},
        { time : Tone.Time('4n'), note : 'A4', dur : '16n'},
        { time : 2 * Tone.Time('4n'), note : 'A4', dur : '16n'},
        { time : 3 * Tone.Time('4n'), note : 'A4', dur : '16n'}]
    );
    started = true;

    part.start(0);

    //loop the part 3 times
    part.loop = 1;
    part.loopEnd = '1m';

    Tone.Transport.schedule(function(time){
      // recorder.stop();
      Tone.Transport.stop();
    }, "2m");

    Tone.Buffer.onload = function() {
			//this will start the player on every quarter note
			Tone.Transport.setInterval(function(time){
			    player.start(time);
			}, "4n");
      //start the Transport for the events to start
      Tone.Transport.start();
		};

    console.log("reach here?")
    console.log(Tone.Transport.bpm.value)
    console.log(Tone.Time('1m').toSeconds())

    // const actx  = Tone.context;
    // const dest  = actx.createMediaStreamDestination();
    // const recorder = new MediaRecorder(dest.stream);

    // // synth.connect(dest);
  
    // const chunks = [];

  
    // recorder.ondataavailable = evt => chunks.push(evt.data);
    // recorder.onstop = evt => {
    //   let blob = new Blob(chunks, { type: 'audio/ogg; codecs=opus' });
    //   audio.src = URL.createObjectURL(blob);
    // };

    // recorder.start();
  }
}