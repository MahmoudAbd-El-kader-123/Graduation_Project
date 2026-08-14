import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { Injectable, PLATFORM_ID, effect, inject, signal } from '@angular/core';
import { PrimeNG } from 'primeng/config';
import { AR_TRANSLATIONS } from './ar.translations';

export type AppLanguage = 'en' | 'ar';

const LANGUAGE_STORAGE_KEY = 'spip_language';
const TRANSLATABLE_ATTRIBUTES = ['aria-label', 'title', 'placeholder', 'label', 'header', 'emptymessage'] as const;

const AR_PRIMENG_TRANSLATION = {
  startsWith: 'يبدأ بـ', contains: 'يحتوي على', notContains: 'لا يحتوي على', endsWith: 'ينتهي بـ',
  equals: 'يساوي', notEquals: 'لا يساوي', noFilter: 'بدون تصفية', lt: 'أقل من', lte: 'أقل من أو يساوي',
  gt: 'أكبر من', gte: 'أكبر من أو يساوي', is: 'هو', isNot: 'ليس', before: 'قبل', after: 'بعد',
  clear: 'مسح', apply: 'تطبيق', matchAll: 'مطابقة الكل', matchAny: 'مطابقة أي', addRule: 'إضافة قاعدة',
  removeRule: 'إزالة القاعدة', accept: 'نعم', reject: 'لا', choose: 'اختيار', upload: 'رفع', cancel: 'إلغاء',
  dayNames: ['الأحد', 'الاثنين', 'الثلاثاء', 'الأربعاء', 'الخميس', 'الجمعة', 'السبت'],
  dayNamesShort: ['أحد', 'اثن', 'ثلا', 'أرب', 'خمي', 'جمع', 'سبت'],
  dayNamesMin: ['ح', 'ن', 'ث', 'ر', 'خ', 'ج', 'س'],
  monthNames: ['يناير', 'فبراير', 'مارس', 'أبريل', 'مايو', 'يونيو', 'يوليو', 'أغسطس', 'سبتمبر', 'أكتوبر', 'نوفمبر', 'ديسمبر'],
  monthNamesShort: ['ينا', 'فبر', 'مار', 'أبر', 'ماي', 'يون', 'يول', 'أغس', 'سبت', 'أكت', 'نوف', 'ديس'],
  dateFormat: 'dd/mm/yy', firstDayOfWeek: 6, today: 'اليوم', weekHeader: 'أسبوع',
  weak: 'ضعيفة', medium: 'متوسطة', strong: 'قوية', passwordPrompt: 'أدخل كلمة المرور',
  emptyMessage: 'لا توجد نتائج', emptyFilterMessage: 'لم يتم العثور على نتائج',
  fileChosenMessage: 'تم اختيار الملف', noFileChosenMessage: 'لم يتم اختيار ملف', pending: 'قيد الانتظار',
  chooseYear: 'اختر السنة', chooseMonth: 'اختر الشهر', chooseDate: 'اختر التاريخ',
  aria: {
    trueLabel: 'نعم', falseLabel: 'لا', nullLabel: 'غير محدد', selectAll: 'تحديد الكل',
    unselectAll: 'إلغاء تحديد الكل', close: 'إغلاق', previous: 'السابق', next: 'التالي', navigation: 'التنقل',
    firstPageLabel: 'الصفحة الأولى', lastPageLabel: 'الصفحة الأخيرة', nextPageLabel: 'الصفحة التالية',
    previousPageLabel: 'الصفحة السابقة', rowsPerPageLabel: 'عدد الصفوف في الصفحة',
    expandRow: 'توسيع الصف', collapseRow: 'طي الصف', showFilterMenu: 'إظهار قائمة التصفية',
    hideFilterMenu: 'إخفاء قائمة التصفية', editRow: 'تعديل الصف', saveEdit: 'حفظ التعديل',
    cancelEdit: 'إلغاء التعديل', browseFiles: 'استعراض الملفات'
  }
};

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly document = inject(DOCUMENT);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly primeNG = inject(PrimeNG);
  private readonly englishPrimeTranslation = {
    ...this.primeNG.translation,
    aria: { ...this.primeNG.translation.aria }
  };
  private readonly originalText = new WeakMap<Text, string>();
  private readonly appliedText = new WeakMap<Text, string>();
  private readonly originalAttributes = new WeakMap<Element, Map<string, string>>();
  private readonly appliedAttributes = new WeakMap<Element, Map<string, string>>();
  private observer?: MutationObserver;

  readonly language = signal<AppLanguage>(this.readInitialLanguage());
  readonly isArabic = () => this.language() === 'ar';

  constructor() {
    if (!isPlatformBrowser(this.platformId)) return;

    effect(() => {
      const language = this.language();
      localStorage.setItem(LANGUAGE_STORAGE_KEY, language);
      this.document.documentElement.lang = language;
      this.document.documentElement.dir = language === 'ar' ? 'rtl' : 'ltr';
      this.document.body.classList.toggle('app-rtl', language === 'ar');
      this.primeNG.setTranslation(language === 'ar' ? AR_PRIMENG_TRANSLATION : this.englishPrimeTranslation);
      this.translateTree(this.document.body);
    });

    this.observer = new MutationObserver(records => {
      for (const record of records) {
        if (record.type === 'characterData' && record.target instanceof Text) {
          this.translateTextNode(record.target);
        }

        if (record.type === 'attributes' && record.target instanceof Element) {
          this.translateElementAttributes(record.target);
        }

        record.addedNodes.forEach(node => this.translateTree(node));
      }
    });

    this.observer.observe(this.document.body, {
      subtree: true,
      childList: true,
      characterData: true,
      attributes: true,
      attributeFilter: [...TRANSLATABLE_ATTRIBUTES]
    });
  }

  setLanguage(language: AppLanguage): void {
    this.language.set(language);
  }

  toggleLanguage(): void {
    this.language.update(language => language === 'en' ? 'ar' : 'en');
  }

  translate(value: string | null | undefined): string {
    if (!value || this.language() === 'en') return value ?? '';
    return this.toArabic(value);
  }

  private readInitialLanguage(): AppLanguage {
    if (!isPlatformBrowser(this.platformId)) return 'en';
    return localStorage.getItem(LANGUAGE_STORAGE_KEY) === 'ar' ? 'ar' : 'en';
  }

  private translateTree(node: Node): void {
    if (node instanceof Text) {
      this.translateTextNode(node);
      return;
    }

    if (!(node instanceof Element) || ['SCRIPT', 'STYLE', 'CODE', 'PRE'].includes(node.tagName)) return;
    this.translateElementAttributes(node);
    node.childNodes.forEach(child => this.translateTree(child));
  }

  private translateTextNode(node: Text): void {
    const current = node.data;
    if (!current.trim()) return;

    const lastApplied = this.appliedText.get(node);
    if (lastApplied !== current) this.originalText.set(node, current);

    const original = this.originalText.get(node) ?? current;
    const next = this.language() === 'ar' ? this.toArabic(original) : original;
    if (next === current) return;

    this.appliedText.set(node, next);
    node.data = next;
  }

  private translateElementAttributes(element: Element): void {
    let originals = this.originalAttributes.get(element);
    let applied = this.appliedAttributes.get(element);
    if (!originals) {
      originals = new Map<string, string>();
      this.originalAttributes.set(element, originals);
    }
    if (!applied) {
      applied = new Map<string, string>();
      this.appliedAttributes.set(element, applied);
    }

    for (const attribute of TRANSLATABLE_ATTRIBUTES) {
      const current = element.getAttribute(attribute);
      if (!current) continue;
      if (applied.get(attribute) !== current) originals.set(attribute, current);

      const original = originals.get(attribute) ?? current;
      const next = this.language() === 'ar' ? this.toArabic(original) : original;
      if (next === current) continue;

      applied.set(attribute, next);
      element.setAttribute(attribute, next);
    }
  }

  private toArabic(value: string): string {
    const leading = value.match(/^\s*/)?.[0] ?? '';
    const trailing = value.match(/\s*$/)?.[0] ?? '';
    const normalized = value.trim().replace(/\s+/g, ' ');
    if (!normalized) return value;

    const exact = AR_TRANSLATIONS[normalized];
    if (exact) return `${leading}${exact}${trailing}`;

    const countMatch = normalized.match(/^(\d+)\s+(difference|differences|different)$/i);
    if (countMatch) return `${leading}${countMatch[1]} اختلافات${trailing}`;

    const pageMatch = normalized.match(/^Showing (\d+) to (\d+) of (\d+) entries$/i);
    if (pageMatch) return `${leading}عرض ${pageMatch[1]} إلى ${pageMatch[2]} من أصل ${pageMatch[3]} سجل${trailing}`;

    return value;
  }
}
