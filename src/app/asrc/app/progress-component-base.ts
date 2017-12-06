import { Observable, Subscription } from 'rxjs/Rx';
import { HostBinding } from '@angular/core';
import { IRemoteProcessSericeStartArgs, IRemoteProcessService } from '../services/remote-service-interface';
import { Router } from '@angular/router';
import { AppStateService } from '../services/appstate.service';
import { HttpErrorResponse } from '@angular/common/http';
import { ScrapeTaskDescriptor, TaskDescriptorBase } from '../common/scrape-task-descriptor';
import { ApplicationState } from '../common/app-state';
import { ComponentBase } from './component-base';



export abstract class ProgressComponentBase extends ComponentBase {
    progressModelBase: ProgressModelBase;
    public setProgressModel(progressModelBase: ProgressModelBase) {
        this.progressModelBase = progressModelBase;
    }

    abstract getServiceStartArgs(): IRemoteProcessSericeStartArgs;

    abstract getService(): IRemoteProcessService;

    constructor(router: Router, appStateService: AppStateService) {
        super(router, appStateService);
        this.progressModelBase = new ProgressModelBase;
    }

    startWork() {
        this.progressModelBase.currentTask = null;
        let args = this.getServiceStartArgs();


        this.getService()
            .startRemoteProcess(args)
            .subscribe(returnedTask => {
                this.updateTaskData(returnedTask);
                this.progressModelBase.inProgress = true;
                this.progressModelBase.scheduler = Observable.interval(1000);
                this.progressModelBase.subscription = this.progressModelBase.scheduler.subscribe((value: number) => {
                    this.refreshTask();
                });
            }, (error: HttpErrorResponse) => {
                this.stopProgress();
                this.progressModelBase.currentTask = args.descriptor;
                this.progressModelBase.currentTask.error = error.message;
            });

    }

    refreshTask() {
        if (!this.progressModelBase.inProgress || this.progressModelBase.currentTask === undefined || this.progressModelBase.isRefreshing) {
            return;
        }
        this.progressModelBase.isRefreshing = true;
        this.getService()
            .updateRemoteProcess(this.progressModelBase.currentTask.guidString)
            .subscribe((updatedTask: ScrapeTaskDescriptor) => {
                this.updateTaskData(updatedTask);
                if ((updatedTask.status && updatedTask.status >= 5) || !!updatedTask.error) {
                    this.stopProgress();
                }
                this.progressModelBase.isRefreshing = false;
            }, (error: HttpErrorResponse) => {
                this.stopProgress();
                this.progressModelBase.currentTask.error = error.message;
                this.progressModelBase.isRefreshing = false;
            });
    }

    updateTaskData(newTask: TaskDescriptorBase) {
        this.progressModelBase.currentTask = newTask;
    }

    stopProgress() {
        if (this.progressModelBase.subscription) {
            this.progressModelBase.subscription.unsubscribe();
            this.progressModelBase.subscription = null;
        }
        this.progressModelBase.scheduler = null;
        this.progressModelBase.inProgress = false;
    }

    cancelTask() {
        if (!this.progressModelBase.inProgress || this.progressModelBase.currentTask === undefined) {
            return;
        }
        this.progressModelBase.isCancelling = true;
        this.stopProgress();
        this.getService().cancelRemoteProcess(this.progressModelBase.currentTask.guidString)
            .subscribe((cancelledTask: ScrapeTaskDescriptor) => {
                this.updateTaskData(cancelledTask);
                this.progressModelBase.isCancelling = false;
            }, (error: HttpErrorResponse) => {
                this.progressModelBase.isCancelling = false;
                this.progressModelBase.currentTask.error = error.message;
            })
    }


    abstract onResetClick();

    onCancelClick() {
        this.cancelTask();
    }




    ngOnInit() {
        super.ngOnInit();
        this.startWork();
    }

    ngOnDestroy() {
        super.ngOnDestroy();

        if (this.progressModelBase.subscription) {
            this.progressModelBase.subscription.unsubscribe();
            this.progressModelBase.subscription = null;
        }
    }

}

export class ProgressModelBase {
    inProgress: boolean = false;
    scheduler: Observable<number>;
    subscription: Subscription;
    isCancelling: boolean = false;
    isRefreshing: boolean = false;
    currentTask: TaskDescriptorBase;
}