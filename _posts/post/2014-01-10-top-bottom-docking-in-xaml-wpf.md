---
title: Top/Bottom docking in XAML / WPF
layout: post
date: 2014-01-10
category: archived
---

You want a layout like:

    ^^ top content
    ^^ content that fills the control vv
    bottom content (nav, actions,etc) vv

`DockPanel` is your friend but just setting `DockPanel.Dock` on the top and bottom controls won't work. By default the last child control in the `DockPanel` fills the panel:

    <DockPanel>
        <Control DockPanel.Dock="Top" x:Name="TopContentControl" />
        <Control DockPanel.Dock="Bottom" x:Name="BottonNavControl" />
        <Control x:Name="FillControl" />
    </DockPanel>

If you've just got content at the top and nav docked to the bottom, the top control can become the fill control. Just remember that is has to be the last control in the `DockPanel`.
    
    <DockPanel>
        <Control DockPanel.Dock="Bottom" x:Name="BottomNavControl" />
        <Control x:Name="TopContentControl" />
    </DockPanel>

Or if you want to keep the same control ordering and explicitly dock the top control to the top, the "last child control in the `DockPanel` filling the panel" default can be turned off.

    <DockPanel LastChildFill="False">
        <Control DockPanel.Dock="Top" x:Name="TopContentControl" />
        <Control DockPanel.Dock="Bottom" x:Name="BottomNavControl" />
    </DockPanel>

