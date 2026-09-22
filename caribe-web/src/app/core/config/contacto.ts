import { environment } from '../../../environments/environment';

/// Datos de contacto del negocio, en un solo lugar.
///
/// El número y el usuario salen de environment.ts: cambiarlos ahí los
/// cambia en el encabezado, el pie, el botón flotante y cada ficha.
export const Contacto = {
  /// Solo dígitos, con código de país: 50688887777.
  whatsapp: environment.whatsapp,

  /// Sin la arroba: importacionescaribecr.
  instagram: environment.instagram,

  /// "+506 8888 7777" — como lo escribe una persona, para mostrarlo.
  get whatsappVisible(): string {
    const d = environment.whatsapp.replace(/\D/g, '');
    return d.length === 11 && d.startsWith('506')
      ? `+506 ${d.slice(3, 7)} ${d.slice(7)}`
      : `+${d}`;
  },

  get instagramUrl(): string {
    return `https://www.instagram.com/${environment.instagram}/`;
  },

  /// Enlace a WhatsApp con un mensaje ya escrito. El cliente solo toca
  /// enviar: cada paso que se le ahorra es un contacto que no se pierde.
  whatsappUrl(texto?: string): string {
    const base = `https://wa.me/${environment.whatsapp.replace(/\D/g, '')}`;
    return texto ? `${base}?text=${encodeURIComponent(texto)}` : base;
  }
};
