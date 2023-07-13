// ======= Copyright 2023 ALBRT.VR contributors. All rights reserved. ===============

// ALBRT.VR project: https://github.com/albrt-vr
// This project: https://github.com/albrt-vr/OpenVR.ALBRT.overlay

// =============== Contributors & Notes ===============
// Created June 2023 by John Penny

// this shader is for filling a frame buffer object with a (full texture) fill that can then be utilised in the frag shader

#version 330 core

out vec2 fill;

const vec2 pos[4]=vec2[4](vec2(-1.0, 1.0),
                          vec2(-1.0,-1.0),
                          vec2( 1.0, 1.0),
                          vec2( 1.0,-1.0));

void main()
{
    // viewport fill
    fill=0.5*pos[gl_VertexID] + vec2(0.5);
    gl_Position=vec4(pos[gl_VertexID], 0.0, 1.0);
}