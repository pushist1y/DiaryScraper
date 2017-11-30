import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';
import { DiaryScraperInputData } from '../common/diary-scraper-input-data';
import { ApplicationState } from '../common/app-state';

@Injectable()
export class AppStateService {

    private appState = new BehaviorSubject<ApplicationState>(new ApplicationState());

    currentState = this.appState.asObservable();

    constructor() { }

    changeState(newState: ApplicationState) {
        this.appState.next(newState);
    }
}