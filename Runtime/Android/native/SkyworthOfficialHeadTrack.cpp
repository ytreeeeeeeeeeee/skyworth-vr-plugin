#include <jni.h>
#include <android/log.h>
#include <dlfcn.h>
#include <pthread.h>
#include <time.h>
#include <math.h>

#define LOG_TAG "SkyworthHeadTrackJNI"
#define LOGI(...) __android_log_print(ANDROID_LOG_INFO, LOG_TAG, __VA_ARGS__)
#define LOGE(...) __android_log_print(ANDROID_LOG_ERROR, LOG_TAG, __VA_ARGS__)

struct avrQuatf {
    float x;
    float y;
    float z;
    float w;
};

typedef int (*StartHeadTrackerFn)(int (*callback)(unsigned long long, avrQuatf&));
typedef void (*StopHeadTrackerFn)();
typedef void (*RecenterHeadTrackerFn)(int);
typedef int (*GetOrientationFn)(double, float*);

static pthread_mutex_t gMutex = PTHREAD_MUTEX_INITIALIZER;
static avrQuatf gLatest = {0.0f, 0.0f, 0.0f, 1.0f};
static int gHasSample = 0;
static int gSampleCount = 0;
static void* gAtwLibrary = nullptr;
static StartHeadTrackerFn gStartHeadTracker = nullptr;
static StopHeadTrackerFn gStopHeadTracker = nullptr;
static RecenterHeadTrackerFn gRecenterHeadTracker = nullptr;
static GetOrientationFn gGetOrientation = nullptr;

static bool ResolveOfficialApi() {
    if (gStopHeadTracker && gRecenterHeadTracker && gGetOrientation) {
        return true;
    }

    gAtwLibrary = dlopen("libatw_api.so", RTLD_NOW);
    if (!gAtwLibrary) {
        LOGE("dlopen libatw_api.so failed: %s", dlerror());
        return false;
    }

    gStartHeadTracker = reinterpret_cast<StartHeadTrackerFn>(
        dlsym(gAtwLibrary, "_Z18awStartHeadTrackerPFiyR9avrQuatf_E"));
    gStopHeadTracker = reinterpret_cast<StopHeadTrackerFn>(
        dlsym(gAtwLibrary, "_Z17awStopHeadTrackerv"));
    gRecenterHeadTracker = reinterpret_cast<RecenterHeadTrackerFn>(
        dlsym(gAtwLibrary, "_Z21awRecenterHeadTrackeri"));
    gGetOrientation = reinterpret_cast<GetOrientationFn>(
        dlsym(gAtwLibrary, "_Z16awGetOrientationdPf"));

    if (!gStopHeadTracker || !gRecenterHeadTracker || !gGetOrientation) {
        LOGE("dlsym failed start=%p stop=%p recenter=%p getOrientation=%p error=%s",
             gStartHeadTracker, gStopHeadTracker, gRecenterHeadTracker, gGetOrientation, dlerror());
        return false;
    }

    return true;
}

static int OnHeadPose(unsigned long long timestampNs, avrQuatf& rotation) {
    (void)timestampNs;

    pthread_mutex_lock(&gMutex);
    gLatest = rotation;
    gHasSample = 1;
    gSampleCount++;
    int sampleCount = gSampleCount;
    pthread_mutex_unlock(&gMutex);

    if (sampleCount == 1) {
        LOGI("first official head tracker sample q=(%f,%f,%f,%f)", rotation.x, rotation.y, rotation.z, rotation.w);
    }

    return 0;
}

extern "C" JNIEXPORT jboolean JNICALL
Java_com_local_skyworth_testvr2021_SkyworthHeadTracker_nativeStart(JNIEnv*, jclass) {
    if (!ResolveOfficialApi()) {
        return JNI_FALSE;
    }

    pthread_mutex_lock(&gMutex);
    gLatest = {0.0f, 0.0f, 0.0f, 1.0f};
    gHasSample = 0;
    gSampleCount = 0;
    pthread_mutex_unlock(&gMutex);

    LOGI("official awGetOrientation polling ready");
    return JNI_TRUE;
}

extern "C" JNIEXPORT void JNICALL
Java_com_local_skyworth_testvr2021_SkyworthHeadTracker_nativeStop(JNIEnv*, jclass) {
    if (ResolveOfficialApi()) {
        gStopHeadTracker();
    }
}

extern "C" JNIEXPORT void JNICALL
Java_com_local_skyworth_testvr2021_SkyworthHeadTracker_nativeRecenter(JNIEnv*, jclass) {
    if (ResolveOfficialApi()) {
        gRecenterHeadTracker(0);
    }
}

extern "C" JNIEXPORT jfloatArray JNICALL
Java_com_local_skyworth_testvr2021_SkyworthHeadTracker_nativeGetQuaternion(JNIEnv* env, jclass) {
    float values[5];

    float orientation[4] = {0.0f, 0.0f, 0.0f, 1.0f};
    int valid = 0;
    if (ResolveOfficialApi()) {
        struct timespec now;
        clock_gettime(CLOCK_MONOTONIC, &now);
        double timeSeconds = static_cast<double>(now.tv_sec) + static_cast<double>(now.tv_nsec) / 1000000000.0;
        gGetOrientation(timeSeconds, orientation);

        float magnitude =
            orientation[0] * orientation[0] +
            orientation[1] * orientation[1] +
            orientation[2] * orientation[2] +
            orientation[3] * orientation[3];
        valid = isfinite(magnitude) && magnitude > 0.25f && magnitude < 4.0f;
        if (valid && !gHasSample) {
            LOGI("first official orientation q=(%f,%f,%f,%f) magnitude=%f",
                 orientation[0], orientation[1], orientation[2], orientation[3], magnitude);
        }
    }

    pthread_mutex_lock(&gMutex);
    if (valid) {
        gLatest.x = orientation[0];
        gLatest.y = orientation[1];
        gLatest.z = orientation[2];
        gLatest.w = orientation[3];
        gHasSample = 1;
    }
    values[0] = gLatest.x;
    values[1] = gLatest.y;
    values[2] = -gLatest.z;
    values[3] = -gLatest.w;
    values[4] = valid ? 1.0f : 0.0f;
    pthread_mutex_unlock(&gMutex);

    jfloatArray array = env->NewFloatArray(5);
    env->SetFloatArrayRegion(array, 0, 5, values);
    return array;
}
