/*
Used to calculate the font color to used based on the background color provided.
*/

export default function(hexColor){

        if(!hexColor){
            return "#000000";
        }

        var color = (hexColor.charAt(0) === '#') ? hexColor.substring(1, 7) : hexColor;
        var r = parseInt(color.substring(0, 2), 16); // hexToR
        var g = parseInt(color.substring(2, 4), 16); // hexToG
        var b = parseInt(color.substring(4, 6), 16); // hexToB
        return (((r * 0.299) + (g * 0.587) + (b * 0.114)) > 186) ?
            "#000000" : "#ffffff";
    
}