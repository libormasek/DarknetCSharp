using DarknetCSharp;

var config = new DarknetConfig("yolov4-tiny.cfg");

using var darknet = new Darknet(config);
