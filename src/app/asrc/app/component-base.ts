import { Component, OnInit, HostBinding } from '@angular/core';
import { Router } from '@angular/router';
import { AppStateService } from '../services/appstate.service';
import { slideInDownAnimation } from './animations';
import { Subscription } from 'rxjs';
import { ApplicationState } from '../common/app-state';
import { DiaryParserInputData } from '../common/diary-parser-input-data';
import { ParseInputDataService } from '../services/parse-input-service';
import { FormControl } from '@angular/forms';


export abstract class ComponentBase {
    @HostBinding('@routeAnimation') routeAnimation = true;
    @HostBinding('style.display') display = 'block';
    constructor(protected router: Router,
        protected appStateService: AppStateService) {

    }

    abstract title: string;
    abstract menuEnabled: boolean;
    protected subscriptions: Array<Subscription> = new Array<Subscription>();
    protected appState: ApplicationState = new ApplicationState();

    ngOnInit() {

        let sub = this.appStateService.currentState.subscribe(newState => this.appState = newState);
        this.subscriptions.push(sub);

        this.appState.menuEnabled = this.menuEnabled;
        this.appState.title = this.title;
        this.appStateService.changeState(this.appState);
    }

    ngOnDestroy() {
        if (!this.subscriptions) {
            return;
        }
        this.subscriptions.forEach((sub) => {
            sub.unsubscribe();
        })
    }
}