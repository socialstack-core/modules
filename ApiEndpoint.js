export default function (path) {
	return (global.apiHost ? global.apiHost : '') + '/v1/' + path;
}