// ======= Copyright 2023 ALBRT.VR contributors. All rights reserved. ===============

// ALBRT.VR project: https://github.com/albrt-vr
// This project: https://github.com/albrt-vr/OpenVR.ALBRT.overlay

// =============== Contributors & Notes ===============
// Created June 2023 by John Penny

// utilise the virtual fill UV to render into the whole texture in memory attached to this frame buffer object
// remember we are pushing an overlay stereo quad per-eye, so each eye only needs half a stereo texture, with the other half being transparent
// we set the left eye to stereo sbs (side by side) and the right to stereo sbs crossed, so in all cases we render solely into the LEFT hand half of the texture

#version 330 core

in vec2 fill;

layout(location = 0) out vec4 outColorLeft;
layout(location = 1) out vec4 outColorRight;

uniform float time;
uniform int maskType; // ALBRT.overlay.cs.Data Enum MaskType
uniform int eye; // ALBRT.overlay.cs.Data Enum Eye

// here a 'slice' is a stepped segment, so each slice contains a slat and a gap
uniform float slatHeight; // % of the slice that is the slat vs gap
uniform float sliceHeight; // % of the viewport that a slice fills - lower means more slices to fill the viewport
uniform float sliceOffset; // % offset

uniform int patchType;
uniform float patchRadialSize;
uniform float patchRadialSoftness;

uniform vec3 slatColour;
uniform vec3 patchColour;

const float safeU = 0.495; // discard the right hand of the texture - plus a little gutter due to UV leaking in stereo textures
const vec4 transparent = vec4(0,0,0,0); // for clarity

float circle(in vec2 uv, in float radius, in float softness){
    vec2 dist = uv;
	return 1. - smoothstep(radius-(radius*softness), radius+(radius*softness), dot(dist,dist)*4.);
}

void main()
{    
    vec2 uv = fill;
    
    outColorLeft = transparent;
    outColorRight = transparent;

    if (maskType == 2) // PATCH
    {
        uv -= vec2(0.25, 0.5); // centre of  the left hand side
        uv.x *= 2.; // we are rendering double wide stereo

        if (uv.x < safeU) // discard the right hand of the texture - plus a little gutter due to UV leaking in stereo textures
        {
            if (patchType == 1) // FLAT
            {
                if (eye == 1) // LEFT
                {
                    outColorLeft = vec4(patchColour.xyz, 1.);
                }

                if (eye == 2) // RIGHT
                {
                    outColorRight = vec4(patchColour.xyz, 1.);
                }

                if (eye == 3) // BOTH
                {
                    outColorLeft = vec4(patchColour.xyz, 1.);
                    outColorRight = vec4(patchColour.xyz, 1.);
                }
            }

            if (patchType == 2) // RADIAL
            {
                float circle = circle(uv, patchRadialSize, patchRadialSoftness);
                float icircle = 1. - circle;

                if (eye == 1) // LEFT
                {
                    outColorLeft = vec4(patchColour.xyz * circle, circle);
                }

                if (eye == 2) // RIGHT
                {
                    outColorRight = vec4(patchColour.xyz * circle, circle);
                }

                if (eye == 3) // BOTH
                {
                    outColorLeft = vec4(patchColour.xyz * circle, circle);
//                    outColorRight = vec4(patchColour.xyz * circle, circle);
                    // NOTE I am very aware of how confusing this is when a user does not clock the eye rendering setting - so I decided to render an inverted patch in B
                    // TODO I think the UX can be improved by going back to the A/B render LEFT/RIGHT eye labelling
                    outColorRight = vec4(patchColour.xyz * icircle, icircle); // inverted
                }
            }
        }
    }
    
    if (maskType == 3) // SLAT
    {
        if (uv.x < safeU) // discard the right hand of the texture - plus a little gutter due to UV leaking in stereo textures
        {
            float frac = fract((uv.y / sliceHeight) + sliceOffset);
            float slats = step(frac, slatHeight);
            float islats = 1. - slats;

            if (eye == 1) // LEFT
            {
                outColorLeft = vec4(slatColour.xyz * slats, slats);
            }

            if (eye == 2) // RIGHT
            {
                outColorRight = vec4(slatColour.xyz * slats, slats);
            }

            if (eye == 3) // BOTH
            {
                outColorLeft = vec4(slatColour.xyz * slats, slats);
                outColorRight = vec4(slatColour.xyz * islats, islats); // inverted
            }
        }
    }

}