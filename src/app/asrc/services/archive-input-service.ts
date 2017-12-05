import { Injectable } from "@angular/core";
import { BehaviorSubject } from 'rxjs/BehaviorSubject';
import { DiaryArchiverInputData } from "../common/diary-archiver-input-data";

@Injectable()
export class ArchiveInputDataService {

    private inputData = new BehaviorSubject<DiaryArchiverInputData>(new DiaryArchiverInputData());

    currentData = this.inputData.asObservable();

    constructor() { }

    changeData(newData: DiaryArchiverInputData) {
        this.inputData.next(newData);
    }
}