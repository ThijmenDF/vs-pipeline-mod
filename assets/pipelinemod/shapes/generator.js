import fs from 'node:fs';

const hub = JSON.parse(fs.readFileSync('pipehub.json'));
const armData = JSON.parse(fs.readFileSync('pipesection.json')).elements[0];

const sets = ['n', 'e', 's', 'w', 'u', 'd'];
const angles = {
    n: { y: -90, z: 0 },
    s: { y: 90, z: 0 },
    e: { y: 180, z: 0 },
    w: { y: 0, z: 0 },
    u: { y: 0, z: -90 },
    d: { y: 0, z: 90 },
};

function combine(sets) {
    const result = [];
    
    function callback(start, current) {
        if (current.length > 0) {
            result.push(current.join(''));
        }
        
        for (let i = start; i < sets.length; i++) {
            current.push(sets[i]);
            callback(i+1, current);
            current.pop();
        }
    }
    
    callback(0, []);
    return result;
}

const combinations = combine(sets);

combinations.forEach(combination => {
    const segments = combination.split('');
    const data = structuredClone(hub);

    segments.forEach(segment => {
        const arm = structuredClone(armData);
        arm.name += '-' + segment;
        arm.children.forEach(child => {
            child.name += '-' + segment;
        });
        
        const r = angles[segment];
        if (r.y !== 0) arm.rotationY = r.y;
        if (r.z !== 0) arm.rotationZ = r.z;
        
        data.elements.push(arm);
    });

    const filename = "pipes/pipesection-" + combination + '.json';

    console.log(filename);
    
    fs.writeFileSync(filename, JSON.stringify(data));
});











