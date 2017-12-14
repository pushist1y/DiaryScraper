import { Component, Inject } from '@angular/core';
import { slideInDownAnimation } from './animations';
import { AppStateService } from '../services/appstate.service';
import { ApplicationState } from '../common/app-state';
import { Router } from '@angular/router';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { Console } from '@angular/core/src/console';

declare var packageJson: any;

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css'],
  animations: [slideInDownAnimation]
})
export class AppComponent {

  constructor(private appStateService: AppStateService,
    private router: Router,
    public dialog: MatDialog) {

  }

  appState: ApplicationState = new ApplicationState();

  ngOnInit() {
    this.appStateService.currentState.subscribe(appState => setTimeout(() => {
      this.appState.menuEnabled = appState.menuEnabled;
      this.appState.title = appState.title;
    }, 0));
    let appState = new ApplicationState();
    appState.title = 'Выгрузка дневников';
    appState.menuEnabled = true;
    this.appStateService.changeState(appState);
  }

  scrapeClick() {
    this.router.navigateByUrl("/input");
  }

  parseClick() {
    this.router.navigateByUrl("/parse");
  }

  archiveClick() {
    this.router.navigateByUrl("/archive");
  }

  aboutClick() {
    let dialogRef = this.dialog.open(DialogOverviewExampleDialog, {
      width: '400px'
    });
  }
}

@Component({
  selector: 'dialog-overview-example-dialog',
  templateUrl: 'about-dialog.html',
  styleUrls: ['./about-dialog.css']
})
export class DialogOverviewExampleDialog {

  constructor(
    public dialogRef: MatDialogRef<DialogOverviewExampleDialog>,
    @Inject(MAT_DIALOG_DATA) public data: any) { }

  getVersion(): string {
    if (packageJson) {
      return packageJson.version;
    }
    return "0";
  }

  onAboutClose(): void {
    console.log('closing');
    this.dialogRef.close();
  }

}
