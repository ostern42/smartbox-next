#!/usr/bin/env python3
"""
Create a test DICOM worklist file for Orthanc
"""

import os
from datetime import datetime

try:
    from pydicom import Dataset
    from pydicom.sequence import Sequence
    from pydicom.uid import generate_uid
except ImportError:
    print("Please install pydicom: pip install pydicom")
    print("You can also use the pre-generated test worklist file: test-worklist.wl")
    exit(1)

def create_test_worklist():
    # Create main dataset
    ds = Dataset()
    
    # Character set
    ds.SpecificCharacterSet = 'ISO_IR 100'
    
    # Patient Module
    ds.PatientName = 'TEST^PATIENT^ONE'
    ds.PatientID = 'TEST123'
    ds.PatientBirthDate = '19800101'
    ds.PatientSex = 'M'
    
    # Study Module
    ds.StudyInstanceUID = generate_uid()
    ds.AccessionNumber = 'ACC001'
    ds.RequestedProcedureDescription = 'Chest X-Ray PA and LAT'
    ds.RequestedProcedureID = 'RP001'
    ds.RequestedProcedurePriority = 'ROUTINE'
    
    # Scheduled Procedure Step Sequence
    sps_seq = Sequence()
    sps = Dataset()
    
    # Scheduled Procedure Step
    sps.Modality = 'CR'  # Computed Radiography
    sps.ScheduledStationAETitle = 'SMARTBOX'
    sps.ScheduledProcedureStepStartDate = datetime.now().strftime('%Y%m%d')
    sps.ScheduledProcedureStepStartTime = '090000'
    sps.ScheduledPerformingPhysicianName = 'DR^SMITH'
    sps.ScheduledProcedureStepDescription = 'Chest X-Ray PA and LAT'
    sps.ScheduledProcedureStepID = 'SPS001'
    sps.ScheduledProtocolCodeSequence = Sequence()  # Empty but required
    
    sps_seq.append(sps)
    ds.ScheduledProcedureStepSequence = sps_seq
    
    # Save as DICOM file
    output_dir = 'orthanc-worklists'
    os.makedirs(output_dir, exist_ok=True)
    
    output_file = os.path.join(output_dir, 'test001.wl')
    ds.save_as(output_file, write_like_original=False)
    
    print(f"Created worklist file: {output_file}")
    print(f"Patient: {ds.PatientName}")
    print(f"Study: {ds.RequestedProcedureDescription}")
    print(f"Date: {sps.ScheduledProcedureStepStartDate}")
    
    return output_file

if __name__ == "__main__":
    create_test_worklist()