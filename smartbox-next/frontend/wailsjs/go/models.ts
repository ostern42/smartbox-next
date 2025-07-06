export namespace dicom {
	
	export class PatientInfo {
	    name: string;
	    id: string;
	    birthDate: string;
	    sex: string;
	
	    static createFrom(source: any = {}) {
	        return new PatientInfo(source);
	    }
	
	    constructor(source: any = {}) {
	        if ('string' === typeof source) source = JSON.parse(source);
	        this.name = source["name"];
	        this.id = source["id"];
	        this.birthDate = source["birthDate"];
	        this.sex = source["sex"];
	    }
	}
	export class StudyInfo {
	    accessionNumber: string;
	    studyDescription: string;
	    referringPhysician: string;
	    performingPhysician: string;
	    institution: string;
	
	    static createFrom(source: any = {}) {
	        return new StudyInfo(source);
	    }
	
	    constructor(source: any = {}) {
	        if ('string' === typeof source) source = JSON.parse(source);
	        this.accessionNumber = source["accessionNumber"];
	        this.studyDescription = source["studyDescription"];
	        this.referringPhysician = source["referringPhysician"];
	        this.performingPhysician = source["performingPhysician"];
	        this.institution = source["institution"];
	    }
	}

}

export namespace main {
	
	export class Camera {
	    id: string;
	    name: string;
	    type: string;
	
	    static createFrom(source: any = {}) {
	        return new Camera(source);
	    }
	
	    constructor(source: any = {}) {
	        if ('string' === typeof source) source = JSON.parse(source);
	        this.id = source["id"];
	        this.name = source["name"];
	        this.type = source["type"];
	    }
	}
	export class SystemInfo {
	    version: string;
	    environment: string;
	    dicomEnabled: boolean;
	
	    static createFrom(source: any = {}) {
	        return new SystemInfo(source);
	    }
	
	    constructor(source: any = {}) {
	        if ('string' === typeof source) source = JSON.parse(source);
	        this.version = source["version"];
	        this.environment = source["environment"];
	        this.dicomEnabled = source["dicomEnabled"];
	    }
	}

}

