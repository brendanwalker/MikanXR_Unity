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

public class MikanStencilBox : global::System.IDisposable {
  private global::System.Runtime.InteropServices.HandleRef swigCPtr;
  protected bool swigCMemOwn;

  internal MikanStencilBox(global::System.IntPtr cPtr, bool cMemoryOwn) {
    swigCMemOwn = cMemoryOwn;
    swigCPtr = new global::System.Runtime.InteropServices.HandleRef(this, cPtr);
  }

  internal static global::System.Runtime.InteropServices.HandleRef getCPtr(MikanStencilBox obj) {
    return (obj == null) ? new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero) : obj.swigCPtr;
  }

  internal static global::System.Runtime.InteropServices.HandleRef swigRelease(MikanStencilBox obj) {
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

  ~MikanStencilBox() {
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
          MikanClientPINVOKE.delete_MikanStencilBox(swigCPtr);
        }
        swigCPtr = new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero);
      }
    }
  }

  public int stencil_id {
    set {
      MikanClientPINVOKE.MikanStencilBox_stencil_id_set(swigCPtr, value);
    } 
    get {
      int ret = MikanClientPINVOKE.MikanStencilBox_stencil_id_get(swigCPtr);
      return ret;
    } 
  }

  public int parent_anchor_id {
    set {
      MikanClientPINVOKE.MikanStencilBox_parent_anchor_id_set(swigCPtr, value);
    } 
    get {
      int ret = MikanClientPINVOKE.MikanStencilBox_parent_anchor_id_get(swigCPtr);
      return ret;
    } 
  }

  public MikanVector3f box_center {
    set {
      MikanClientPINVOKE.MikanStencilBox_box_center_set(swigCPtr, MikanVector3f.getCPtr(value));
    } 
    get {
      global::System.IntPtr cPtr = MikanClientPINVOKE.MikanStencilBox_box_center_get(swigCPtr);
      MikanVector3f ret = (cPtr == global::System.IntPtr.Zero) ? null : new MikanVector3f(cPtr, false);
      return ret;
    } 
  }

  public MikanVector3f box_x_axis {
    set {
      MikanClientPINVOKE.MikanStencilBox_box_x_axis_set(swigCPtr, MikanVector3f.getCPtr(value));
    } 
    get {
      global::System.IntPtr cPtr = MikanClientPINVOKE.MikanStencilBox_box_x_axis_get(swigCPtr);
      MikanVector3f ret = (cPtr == global::System.IntPtr.Zero) ? null : new MikanVector3f(cPtr, false);
      return ret;
    } 
  }

  public MikanVector3f box_y_axis {
    set {
      MikanClientPINVOKE.MikanStencilBox_box_y_axis_set(swigCPtr, MikanVector3f.getCPtr(value));
    } 
    get {
      global::System.IntPtr cPtr = MikanClientPINVOKE.MikanStencilBox_box_y_axis_get(swigCPtr);
      MikanVector3f ret = (cPtr == global::System.IntPtr.Zero) ? null : new MikanVector3f(cPtr, false);
      return ret;
    } 
  }

  public MikanVector3f box_z_axis {
    set {
      MikanClientPINVOKE.MikanStencilBox_box_z_axis_set(swigCPtr, MikanVector3f.getCPtr(value));
    } 
    get {
      global::System.IntPtr cPtr = MikanClientPINVOKE.MikanStencilBox_box_z_axis_get(swigCPtr);
      MikanVector3f ret = (cPtr == global::System.IntPtr.Zero) ? null : new MikanVector3f(cPtr, false);
      return ret;
    } 
  }

  public float box_x_size {
    set {
      MikanClientPINVOKE.MikanStencilBox_box_x_size_set(swigCPtr, value);
    } 
    get {
      float ret = MikanClientPINVOKE.MikanStencilBox_box_x_size_get(swigCPtr);
      return ret;
    } 
  }

  public float box_y_size {
    set {
      MikanClientPINVOKE.MikanStencilBox_box_y_size_set(swigCPtr, value);
    } 
    get {
      float ret = MikanClientPINVOKE.MikanStencilBox_box_y_size_get(swigCPtr);
      return ret;
    } 
  }

  public float box_z_size {
    set {
      MikanClientPINVOKE.MikanStencilBox_box_z_size_set(swigCPtr, value);
    } 
    get {
      float ret = MikanClientPINVOKE.MikanStencilBox_box_z_size_get(swigCPtr);
      return ret;
    } 
  }

  public bool is_disabled {
    set {
      MikanClientPINVOKE.MikanStencilBox_is_disabled_set(swigCPtr, value);
    } 
    get {
      bool ret = MikanClientPINVOKE.MikanStencilBox_is_disabled_get(swigCPtr);
      return ret;
    } 
  }

  public string stencil_name {
    set {
      MikanClientPINVOKE.MikanStencilBox_stencil_name_set(swigCPtr, value);
    } 
    get {
      string ret = MikanClientPINVOKE.MikanStencilBox_stencil_name_get(swigCPtr);
      return ret;
    } 
  }

  public MikanStencilBox() : this(MikanClientPINVOKE.new_MikanStencilBox(), true) {
  }

}

}
