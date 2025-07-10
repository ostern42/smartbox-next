#!/usr/bin/env python3
"""
Convert JSON format worklist to DICOM worklist file
Usage: python json2dcm.py input.json output.wl
"""

import json
import sys
from datetime import datetime

try:
    import pydicom
    from pydicom.dataset import Dataset, FileDataset
    from pydicom.sequence import Sequence
except ImportError:
    print("Please install pydicom: pip install pydicom")
    sys.exit(1)

def json_to_dicom_worklist(json_file, output_file):
    # Read JSON
    with open(json_file, 'r') as f:
        data = json.load(f)
    
    # Create main dataset
    ds = Dataset()
    
    # Basic patient info
    ds.SpecificCharacterSet = 'ISO_IR 100'
    ds.AccessionNumber = 'ACC001'
    ds.PatientName = 'TEST^PATIENT'
    ds.PatientID = 'TEST123' 
    ds.PatientBirthDate = '19800101'
    ds.PatientSex = 'M'
    ds.StudyInstanceUID = '1.2.3.4.5.6.7.8.9'
    ds.RequestedProcedureDescription = 'Chest X-Ray'
    
    # Scheduled Procedure Step Sequence
    sps_seq = Sequence()
    sps_item = Dataset()
    sps_item.Modality = 'CR'
    sps_item.ScheduledStationAETitle = 'SMARTBOX'
    sps_item.ScheduledProcedureStepStartDate = datetime.now().strftime('%Y%m%d')
    sps_item.ScheduledProcedureStepStartTime = '090000'
    sps_item.ScheduledPerformingPhysicianName = 'DR^SMITH'
    sps_item.ScheduledProcedureStepDescription = 'Chest X-Ray PA and LAT'
    sps_item.ScheduledProtocolCodeSequence = Sequence()
    sps_item.ScheduledProcedureStepID = 'SPS001'
    sps_seq.append(sps_item)
    ds.ScheduledProcedureStepSequence = sps_seq
    
    ds.RequestedProcedureID = 'RP001'
    ds.RequestedProcedurePriority = 'NORMAL'
    
    # Save as DICOM
    ds.is_implicit_VR = True
    ds.is_little_endian = True
    ds.save_as(output_file, write_like_original=False)
    print(f"Created worklist file: {output_file}")

if __name__ == "__main__":
    if len(sys.argv) != 3:
        print("Usage: python json2dcm.py input.json output.wl")
        sys.exit(1)
    
    json_to_dicom_worklist(sys.argv[1], sys.argv[2])