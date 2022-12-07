package com.countly.nativecrashlib;

import android.util.Log;

import androidx.annotation.NonNull;

import java.io.PrintWriter;
import java.io.StringWriter;
import java.util.Map;

public class NativeCrashHandler {

    public NativeCrashHandler() {
    }

    public void throwException() {
        int a = 5;
        int b = 0;
        int c = a / b;
    }

}
