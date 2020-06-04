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

  freqEnv: [],
  started: false,

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

  init(play) {
    Tone.Transport.bpm.value = bpm;
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
  },

  createAndRecordSequence(bpm, notes, numBars, audio) {
    // simple check to avoid double play
    if (this.started) return;
    var player = new Tone.Player('https://cdn.jsdelivr.net/gh/R-D-D-D/Ground-Zero/web_frontend/src/resources/metronome_click.mp3');
    player.toMaster();

    Tone.Transport.bpm.value = bpm;
    Tone.Context.latencyHint = 'fastest';
    const part = new Tone.Part((time) => {
      //the events will be given to the callback with the time they occur
      this.poly.voices.forEach((v, i) => {
        this.freqEnv[i].triggerAttackRelease('16n', time);
        v.envelope.triggerAttackRelease('16n', time);
      });
      this.noise.triggerAttackRelease('16n', time);
      }, [0, Tone.Time('4n'), 2 * Tone.Time('4n'), 3 * Tone.Time('4n')]
    );
    this.started = true;

    part.start(0);

    //loop the part 3 times
    part.loop = 1;
    part.loopEnd = '1m';

    Tone.Transport.schedule((time) => {
      // recorder.stop();
      console.log('stopped')
      Tone.Transport.stop();
      this.started = false;
    }, "2m");

    Tone.Transport.scheduleRepeat(() => {
      console.log(player.loaded)
      player.restart();
    }, "4n", "0m");

    Tone.Transport.start();

    // var seq = new Tone.Sequence((time, note) => {
    //   this.player.get("click").start();
    // //straight quater notes
    // }, [1, 2, 3, 4, 5, 6, 7, 8], "8n");
    //start the Transport for the events to start
    
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