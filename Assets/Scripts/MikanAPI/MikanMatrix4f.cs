//------------------------------------------------------------------------------
// <auto-generated />
//
// This file was automatically generated by SWIG (https://www.swig.org).
// Version 4.1.1
//
// Do not make changes to this file unless you know what you are doing - modify
// the SWIG interface file instead.
//------------------------------------------------------------------------------

namespace Mikan {

public class MikanMatrix4f : global::System.IDisposable {
  private global::System.Runtime.InteropServices.HandleRef swigCPtr;
  protected bool swigCMemOwn;

  internal MikanMatrix4f(global::System.IntPtr cPtr, bool cMemoryOwn) {
    swigCMemOwn = cMemoryOwn;
    swigCPtr = new global::System.Runtime.InteropServices.HandleRef(this, cPtr);
  }

  internal static global::System.Runtime.InteropServices.HandleRef getCPtr(MikanMatrix4f obj) {
    return (obj == null) ? new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero) : obj.swigCPtr;
  }

  internal static global::System.Runtime.InteropServices.HandleRef swigRelease(MikanMatrix4f obj) {
    if (obj != null) {
      if (!obj.swigCMemOwn)
        throw new global::System.ApplicationException("Cannot release ownership as memory is not owned");
      global::System.Runtime.InteropServices.HandleRef ptr = obj.swigCPtr;
      obj.swigCMemOwn = false;
      obj.Dispose();
      return ptr;
    } else {
      return new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero);
    }
  }

  ~MikanMatrix4f() {
    Dispose(false);
  }

  public void Dispose() {
    Dispose(true);
    global::System.GC.SuppressFinalize(this);
  }

  protected virtual void Dispose(bool disposing) {
    lock(this) {
      if (swigCPtr.Handle != global::System.IntPtr.Zero) {
        if (swigCMemOwn) {
          swigCMemOwn = false;
          MikanClientPINVOKE.delete_MikanMatrix4f(swigCPtr);
        }
        swigCPtr = new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero);
      }
    }
  }

  public float x0 {
    set {
      MikanClientPINVOKE.MikanMatrix4f_x0_set(swigCPtr, value);
    } 
    get {
      float ret = MikanClientPINVOKE.MikanMatrix4f_x0_get(swigCPtr);
      return ret;
    } 
  }

  public float x1 {
    set {
      MikanClientPINVOKE.MikanMatrix4f_x1_set(swigCPtr, value);
    } 
    get {
      float ret = MikanClientPINVOKE.MikanMatrix4f_x1_get(swigCPtr);
      return ret;
    } 
  }

  public float x2 {
    set {
      MikanClientPINVOKE.MikanMatrix4f_x2_set(swigCPtr, value);
    } 
    get {
      float ret = MikanClientPINVOKE.MikanMatrix4f_x2_get(swigCPtr);
      return ret;
    } 
  }

  public float x3 {
    set {
      MikanClientPINVOKE.MikanMatrix4f_x3_set(swigCPtr, value);
    } 
    get {
      float ret = MikanClientPINVOKE.MikanMatrix4f_x3_get(swigCPtr);
      return ret;
    } 
  }

  public float y0 {
    set {
      MikanClientPINVOKE.MikanMatrix4f_y0_set(swigCPtr, value);
    } 
    get {
      float ret = MikanClientPINVOKE.MikanMatrix4f_y0_get(swigCPtr);
      return ret;
    } 
  }

  public float y1 {
    set {
      MikanClientPINVOKE.MikanMatrix4f_y1_set(swigCPtr, value);
    } 
    get {
      float ret = MikanClientPINVOKE.MikanMatrix4f_y1_get(swigCPtr);
      return ret;
    } 
  }

  public float y2 {
    set {
      MikanClientPINVOKE.MikanMatrix4f_y2_set(swigCPtr, value);
    } 
    get {
      float ret = MikanClientPINVOKE.MikanMatrix4f_y2_get(swigCPtr);
      return ret;
    } 
  }

  public float y3 {
    set {
      MikanClientPINVOKE.MikanMatrix4f_y3_set(swigCPtr, value);
    } 
    get {
      float ret = MikanClientPINVOKE.MikanMatrix4f_y3_get(swigCPtr);
      return ret;
    } 
  }

  public float z0 {
    set {
      MikanClientPINVOKE.MikanMatrix4f_z0_set(swigCPtr, value);
    } 
    get {
      float ret = MikanClientPINVOKE.MikanMatrix4f_z0_get(swigCPtr);
      return ret;
    } 
  }

  public float z1 {
    set {
      MikanClientPINVOKE.MikanMatrix4f_z1_set(swigCPtr, value);
    } 
    get {
      float ret = MikanClientPINVOKE.MikanMatrix4f_z1_get(swigCPtr);
      return ret;
    } 
  }

  public float z2 {
    set {
      MikanClientPINVOKE.MikanMatrix4f_z2_set(swigCPtr, value);
    } 
    get {
      float ret = MikanClientPINVOKE.MikanMatrix4f_z2_get(swigCPtr);
      return ret;
    } 
  }

  public float z3 {
    set {
      MikanClientPINVOKE.MikanMatrix4f_z3_set(swigCPtr, value);
    } 
    get {
      float ret = MikanClientPINVOKE.MikanMatrix4f_z3_get(swigCPtr);
      return ret;
    } 
  }

  public float w0 {
    set {
      MikanClientPINVOKE.MikanMatrix4f_w0_set(swigCPtr, value);
    } 
    get {
      float ret = MikanClientPINVOKE.MikanMatrix4f_w0_get(swigCPtr);
      return ret;
    } 
  }

  public float w1 {
    set {
      MikanClientPINVOKE.MikanMatrix4f_w1_set(swigCPtr, value);
    } 
    get {
      float ret = MikanClientPINVOKE.MikanMatrix4f_w1_get(swigCPtr);
      return ret;
    } 
  }

  public float w2 {
    set {
      MikanClientPINVOKE.MikanMatrix4f_w2_set(swigCPtr, value);
    } 
    get {
      float ret = MikanClientPINVOKE.MikanMatrix4f_w2_get(swigCPtr);
      return ret;
    } 
  }

  public float w3 {
    set {
      MikanClientPINVOKE.MikanMatrix4f_w3_set(swigCPtr, value);
    } 
    get {
      float ret = MikanClientPINVOKE.MikanMatrix4f_w3_get(swigCPtr);
      return ret;
    } 
  }

  public MikanMatrix4f() : this(MikanClientPINVOKE.new_MikanMatrix4f(), true) {
  }

}

}
