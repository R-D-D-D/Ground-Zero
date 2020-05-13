$ = x => document.getElementById(x);

comps = {
  load: (x) => {
    if (!comps[x]) {
      comps[x] = {};
    }
      comps[x].template = `<div id="${x}">${$(x).innerHTML}</div>`;
    },
  
    update: (x, obj) => {
      if (!comps[x]) {
        comps[x] = obj;
      } else {
        Object.keys(obj).forEach(key => {
          comps[x][key] = obj[key];
        });
      }
    },
  };

function getRandomInt(max) {
  return Math.floor(Math.random() * Math.floor(max));
}