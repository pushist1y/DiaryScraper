import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { DiaryParseInputComponent } from './diary-parse-input.component';

describe('DiaryParseInputComponent', () => {
  let component: DiaryParseInputComponent;
  let fixture: ComponentFixture<DiaryParseInputComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ DiaryParseInputComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(DiaryParseInputComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
