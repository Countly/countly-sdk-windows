package com.example.crashlib;

import android.util.Log;

import androidx.annotation.NonNull;

import java.io.PrintWriter;
import java.io.StringWriter;
import java.util.Map;

public class CrashHandler {

    public CrashHandler() {

        enableCrashReporting();
    }
    void enableCrashReporting() {
        Log.d("[Countly][Android]","[ModuleCrash] Enabling unhandled crash reporting");
        //get default handler
        final Thread.UncaughtExceptionHandler oldHandler = Thread.getDefaultUncaughtExceptionHandler();

        Thread.UncaughtExceptionHandler handler = new Thread.UncaughtExceptionHandler() {

            @Override
            public void uncaughtException(@NonNull Thread t, @NonNull Throwable e) {
                Log.d("[Countly][Android]", "[ModuleCrash] Uncaught crash handler triggered");
                StringWriter sw = new StringWriter();
                PrintWriter pw = new PrintWriter(sw);
                e.printStackTrace(pw);


                String exceptionString = sw.toString();

                //sendCrashReportToQueue(exceptionString, false, false, null);
                Log.d("[Countly][Android]", exceptionString);

                //if there was another handler before
                if (oldHandler != null) {
                    //notify it also
                    oldHandler.uncaughtException(t, e);
                }
            }
        };

        Thread.setDefaultUncaughtExceptionHandler(handler);
    }

    void addAllThreadInformationToCrash(PrintWriter pw) {
        Map<Thread, StackTraceElement[]> allThreads = Thread.getAllStackTraces();

        for (Map.Entry<Thread, StackTraceElement[]> entry : allThreads.entrySet()) {
            StackTraceElement[] val = entry.getValue();
            Thread thread = entry.getKey();

            if (val == null || thread == null) {
                continue;
            }

            pw.println();
            pw.println("Thread " + thread.getName());
            for (StackTraceElement stackTraceElement : val) {
                pw.println(stackTraceElement.toString());
            }
        }
    }

}
