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
  player: null,

  notesToEvents(notes) {
    var result = [];
    var time = 0;
    notes.forEach(element => {
      var temp = element.split("/")[2];
      var isRest = false;
      var isDot = false;
      if (temp.includes("d") || temp.includes("r")) {
        if (temp.includes("r")) {
          temp = temp.replace("r", "");
          isRest = true;
        }
        if (temp.includes("d")) {
          temp = temp.replace("d", "");
          isDot = true;
        }
      }
      if (temp.includes("q")) {
        temp = '4';
      } else if (temp.includes("h")) {
        temp = '2';
      } else if (temp.includes("f")) {
        temp = '4';
      } 
      temp += 'n';
      if (isDot) temp += "."
      if (!isRest) {
        result.push(time);
      }
      time += Tone.Time(temp);
    });
    return result;
  },

  init(play) {
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
    if (this.player == null) {
      this.player = new Tone.Player('https://cdn.jsdelivr.net/gh/R-D-D-D/Ground-Zero/web_frontend/src/resources/metronome_click.mp3');
      this.player.toMaster();
    }
  },

  createAndRecordSequence(bpm, notes, numBars, audio) {
    Tone.Transport.bpm.value = bpm;
    this.notesToEvents([])
    // simple check to avoid double play
    if (this.started) return;
    this.started = true;

    Tone.Context.latencyHint = 'fastest';
    const part = new Tone.Part((time) => {
      //the events will be given to the callback with the time they occur
      this.poly.voices.forEach((v, i) => {
        this.freqEnv[i].triggerAttackRelease('16n', time);
        v.envelope.triggerAttackRelease('16n', time);
      });
      this.noise.triggerAttackRelease('16n', time);
      }, this.notesToEvents(notes)
    );

    part.start('1m');

    //loop the part 1 time
    part.loop = 1;
    var totalNumBars = numBars + "m";

    
    Tone.Transport.scheduleRepeat(() => {
      this.player.restart();
    }, "4n", "0m");

    // var seq = new Tone.Sequence((time, note) => {
    //   this.player.get("click").start();
    // //straight quater notes
    // }, [1, 2, 3, 4, 5, 6, 7, 8], "8n");
    //start the Transport for the events to start

    const actx  = Tone.context;
    const dest  = actx.createMediaStreamDestination();
    const recorder = new MediaRecorder(dest.stream);

    this.lowPass.connect(dest);
    this.poly.connect(dest);
    this.player.connect(dest);
  
    const chunks = [];

    recorder.ondataavailable = evt => chunks.push(evt.data);
    recorder.onstop = evt => {
      let blob = new Blob(chunks, { type: 'audio/ogg; codecs=opus' });
      audio.src = URL.createObjectURL(blob);
    };

    
    Tone.Transport.schedule((time) => {
      recorder.stop();
      console.log('stopped')
      Tone.Transport.stop();
      this.started = false;
    }, totalNumBars);

    recorder.start();
    Tone.Transport.start();
  }
}