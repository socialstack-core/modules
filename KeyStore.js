import { ec } from 'UI/Functions/Elliptic';
import store from 'UI/Functions/Store';
import sha256 from 'UI/Functions/Sha256';

function toBase64(u8){
    return btoa(String.fromCharCode.apply(null, u8));
}

function fromBase64(b64encoded){
    return atob(b64encoded).split("").map(c => c.charCodeAt(0));
}

function createPair(){
	return new ec('secp256k1').genKeyPair();
}

function storeNewKey(){
    var pair = createPair();
    keys.push({priv: toBase64(pair.getPrivate().toArray())});
	store.set('keys', keys);
    return pair;
}

var keys = store.get('keys');
var currentKey = null;

if(!keys || !keys.length){
    keys = [];
    currentKey = storeNewKey();
}else{
    keys.forEach(key => {
		key.pair = new ec('secp256k1').keyFromPrivate(fromBase64(key.priv));
    });
    currentKey = keys[keys.length-1].pair;
}

module.exports = {
    currentKey: () => currentKey,
    createPair,
    publicKey: () => toBase64(currentKey.getPublic().encode()),
    sign: (message)=> toBase64(currentKey.sign(sha256(message, { asBytes: true })).toDER()),
    toBase64,
    fromBase64
};